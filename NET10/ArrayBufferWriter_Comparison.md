# ArrayBufferWriter vs 기존 방식 비교

## 기존 방식 (Serialize → byte[])

```csharp
// ❌ 메모리 할당이 많은 방식
var responseBody = MessagePackSerializer.Serialize(response);
var responsePacket = new byte[4 + responseBody.Length];
var responseSpan = responsePacket.AsSpan();

// 헤더 작성
BinaryPrimitives.WriteUInt16BigEndian(responseSpan.Slice(0, 2), messageType);
BinaryPrimitives.WriteUInt16BigEndian(responseSpan.Slice(2, 2), (ushort)responseBody.Length);

// 바디 복사
responseBody.CopyTo(responseSpan.Slice(4));
```

**메모리 할당 과정**:
1. `MessagePack 내부 임시 버퍼` - 직렬화 중 사용 (예: 512바이트)
2. `responseBody` - 최종 직렬화 결과 (예: 250바이트)
3. `responsePacket` - 헤더 + 바디 (예: 254바이트)

**총 할당**: ~1016바이트 (임시 버퍼 512 + responseBody 250 + responsePacket 254)
**GC 대상**: 임시 버퍼 512바이트, responseBody 250바이트

---

## ArrayBufferWriter 방식

```csharp
// ✅ 메모리 할당이 적은 방식
var bufferWriter = new ArrayBufferWriter<byte>();
MessagePackSerializer.Serialize(bufferWriter, response);
var responseBodySpan = bufferWriter.WrittenSpan;

// 헤더 + 바디를 위한 버퍼 (ArrayPool 사용)
var totalLength = 4 + responseBodySpan.Length;
var rentedBuffer = ArrayPool<byte>.Shared.Rent(totalLength);
var responseSpan = rentedBuffer.AsSpan(0, totalLength);

// 헤더 작성
BinaryPrimitives.WriteUInt16BigEndian(responseSpan.Slice(0, 2), messageType);
BinaryPrimitives.WriteUInt16BigEndian(responseSpan.Slice(2, 2), (ushort)responseBodySpan.Length);

// 바디 복사
responseBodySpan.CopyTo(responseSpan.Slice(4));

// 전송 버퍼 생성
var sendBuffer = new byte[totalLength];
responseSpan.CopyTo(sendBuffer);

// ArrayPool 반환 (재사용 가능)
ArrayPool<byte>.Shared.Return(rentedBuffer);
```

**메모리 할당 과정**:
1. `ArrayBufferWriter 내부 버퍼` - 초기 256바이트, 필요 시 확장
   - 이 버퍼는 WrittenSpan으로 직접 접근 (추가 복사 없음)
2. `rentedBuffer` - ArrayPool에서 빌림 (**할당 아님, 재사용**)
3. `sendBuffer` - 최종 전송 버퍼 (254바이트)

**총 할당**: ~510바이트 (ArrayBufferWriter 내부 256 + sendBuffer 254)
**GC 대상**: ArrayBufferWriter 내부 버퍼 256바이트
**재사용**: rentedBuffer는 ArrayPool로 반환되어 다음 요청에서 재사용

---

## 핵심 차이점

### 1. **중간 복사 제거**

**기존 방식**:
```
MessagePack 내부 버퍼 → responseBody → responsePacket
         복사             복사
```

**ArrayBufferWriter**:
```
ArrayBufferWriter 내부 버퍼 → (WrittenSpan으로 직접 참조) → rentedBuffer → sendBuffer
                                           복사              복사
```

### 2. **ArrayPool 활용**

**기존 방식**:
- 모든 버퍼를 새로 할당
- GC가 모든 임시 버퍼를 정리해야 함

**ArrayBufferWriter + ArrayPool**:
- rentedBuffer는 풀에서 빌려서 사용 후 반환
- 다음 요청 시 동일한 버퍼 재사용 가능
- GC 압력 크게 감소

### 3. **메모리 효율성**

| 항목 | 기존 방식 | ArrayBufferWriter 방식 |
|------|-----------|----------------------|
| 총 할당 | ~1016 bytes | ~510 bytes |
| GC 대상 | ~762 bytes | ~256 bytes |
| 재사용 버퍼 | 없음 | rentedBuffer (ArrayPool) |
| 복사 횟수 | 2회 | 2회 (동일) |

---

## ArrayBufferWriter의 동작 원리

```csharp
// 초기 상태
ArrayBufferWriter<byte> writer = new();
// 내부 버퍼: [                ] (256 bytes, 초기 크기)

// MessagePack이 50바이트 쓰기 요청
var span = writer.GetSpan(50);
// → 내부 버퍼 반환: [________        ]
//                    ↑ 50바이트 공간

// 실제로 40바이트만 사용
writer.Advance(40);
// → [XXXX____        ]
//    ↑ 40바이트 사용됨

// 추가로 300바이트 쓰기 요청 (남은 공간 216바이트 부족)
var span2 = writer.GetSpan(300);
// → 버퍼 확장 (기존 데이터 복사)
// → [XXXX                    ] (512 bytes)
//         ↑ 300바이트 공간

// 실제로 200바이트만 사용
writer.Advance(200);
// → [XXXXYYYYYYYYYYY         ]
//    ↑40  ↑200

// 최종 결과
var result = writer.WrittenSpan;
// → 정확히 240바이트만 포함 (40 + 200)
```

---

## 실제 성능 비교 (1000건 처리 시)

### 기존 방식
```
총 할당: 1,016,000 bytes (~1MB)
GC 수집: 762,000 bytes
GC 발생: 높음 (Gen0 자주 발생)
```

### ArrayBufferWriter + ArrayPool
```
총 할당: 510,000 bytes (~498KB)
GC 수집: 256,000 bytes
재사용: rentedBuffer를 통해 ~254KB 재사용
GC 발생: 낮음 (Gen0 발생 감소)
```

**결과**: 메모리 할당 50% 감소, GC 압력 66% 감소

---

## 결론

ArrayBufferWriter는:
1. **직렬화 중 임시 버퍼 생성을 최소화**
2. **WrittenSpan으로 직접 접근하여 불필요한 복사 제거**
3. **ArrayPool과 함께 사용하여 버퍼 재사용**
4. **GC 압력을 크게 줄여 전체 성능 향상**

특히 고성능 서버 환경에서는 이러한 최적화가 **처리량(throughput) 향상**과 **지연시간(latency) 감소**로 이어집니다.