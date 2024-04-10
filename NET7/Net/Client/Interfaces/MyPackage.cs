namespace Client.Interfaces
{
    /// <summary>
    /// | BodyLength | Id    | Type  | Body ...   |
    /// |  2Byte     | 2Byte | 1Byte | BodyLength |
    /// | 0        1 | 2   3 | 4     | 5 ...      |
    /// Header: 2 + 2 + 1 = 5Bytes
    /// Total: Header + BodyLength
    /// </summary>
    public class MyPackage
    {
        public ushort BodyLength { get; set; }
        public ushort Id { get; set; }
        public byte Type { get; set; }
        public byte[] Body { get; set; }
    }
}
