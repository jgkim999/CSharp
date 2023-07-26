using Client;

using MessagePack;

using System;
using System.Collections.Generic;
using System.IO;

namespace WpfClient.Net
{
    public class PacketDef
    {
        public const short PACKET_HEADER_SIZE = 5;
        public const int MAX_USER_ID_BYTE_LENGTH = 16;
        public const int MAX_USER_PW_BYTE_LENGTH = 16;

        public const int INVALID_ROOM_NUMBER = -1;
    }

    public class PacketToBytes
    {
        public static byte[] Make(ushort packetID, MemoryStream ms)
        {
            byte type = 0;
            var pktID = packetID;
            ushort bodyDataSize = (ushort)ms.Length;
            var packetSize = (ushort)(bodyDataSize + PacketDef.PACKET_HEADER_SIZE);

            //var dataSource = new byte[packetSize];
            var dataSource = ByteArrayPool.Get(packetSize);
            if (BitConverter.IsLittleEndian)
            {
                var sizeArray = BitConverter.GetBytes(bodyDataSize);
                Array.Reverse(sizeArray);
                System.Buffer.BlockCopy(sizeArray, 0, dataSource, 0, 2);

                var idArray = BitConverter.GetBytes(pktID);
                Array.Reverse(idArray);
                System.Buffer.BlockCopy(idArray, 0, dataSource, 2, 2);
            }
            else
            {
                System.Buffer.BlockCopy(BitConverter.GetBytes(bodyDataSize), 0, dataSource, 0, 2);
                System.Buffer.BlockCopy(BitConverter.GetBytes(pktID), 0, dataSource, 2, 2);
            }
            dataSource[4] = type;

            System.Buffer.BlockCopy(ms.GetBuffer(), 0, dataSource, 5, bodyDataSize);

            return dataSource;
        }

        public static byte[] Make(ushort packetID, byte[] bodyData)
        {
            byte type = 0;
            var pktID = (ushort)packetID;
            ushort bodyDataSize = 0;
            if (bodyData != null)
            {
                bodyDataSize = (ushort)bodyData.Length;
            }
            var packetSize = (ushort)(bodyDataSize + PacketDef.PACKET_HEADER_SIZE);

            //var dataSource = new byte[packetSize];
            var dataSource = ByteArrayPool.Get(packetSize);
            if (BitConverter.IsLittleEndian)
            {
                var sizeArray = BitConverter.GetBytes(bodyDataSize);
                Array.Reverse(sizeArray);
                System.Buffer.BlockCopy(sizeArray, 0, dataSource, 0, 2);

                var idArray = BitConverter.GetBytes(pktID);
                Array.Reverse(idArray);
                System.Buffer.BlockCopy(idArray, 0, dataSource, 2, 2);
            }
            else
            {
                System.Buffer.BlockCopy(BitConverter.GetBytes(bodyDataSize), 0, dataSource, 0, 2);
                System.Buffer.BlockCopy(BitConverter.GetBytes(pktID), 0, dataSource, 2, 2);
            }
            dataSource[4] = type;

            if (bodyData != null)
            {
                System.Buffer.BlockCopy(bodyData, 0, dataSource, 5, bodyDataSize);
            }

            return dataSource;
        }

        public static Tuple<int, byte[]> ClientReceiveData(int recvLength, byte[] recvData)
        {
            var packetSize = BitConverter.ToUInt16(recvData, 0);
            var packetID = BitConverter.ToUInt16(recvData, 2);
            var bodySize = packetSize - PacketDef.PACKET_HEADER_SIZE;

            var packetBody = new byte[bodySize];
            System.Buffer.BlockCopy(recvData, PacketDef.PACKET_HEADER_SIZE, packetBody, 0, bodySize);

            return new Tuple<int, byte[]>(packetID, packetBody);
        }
    }

    [MessagePackObject]
    public class PKTEcho
    {
        [Key(0)]
        public string Message { get; set; }
    }

    // 로그인 요청
    [MessagePackObject]
    public class PKTReqLogin
    {
        [Key(0)]
        public string UserID;
        [Key(1)]
        public string AuthToken;
    }

    [MessagePackObject]
    public class PKTResLogin
    {
        [Key(0)]
        public short Result;
    }


    [MessagePackObject]
    public class PKNtfMustClose
    {
        [Key(0)]
        public short Result;
    }

    [MessagePackObject]
    public class PKTReqRoomEnter
    {
        [Key(0)]
        public int RoomNumber;
    }

    [MessagePackObject]
    public class PKTResRoomEnter
    {
        [Key(0)]
        public short Result;
    }

    [MessagePackObject]
    public class PKTNtfRoomUserList
    {
        [Key(0)]
        public List<string> UserIDList = new List<string>();
    }

    [MessagePackObject]
    public class PKTNtfRoomNewUser
    {
        [Key(0)]
        public string UserID;
    }


    [MessagePackObject]
    public class PKTReqRoomLeave
    {
    }

    [MessagePackObject]
    public class PKTResRoomLeave
    {
        [Key(0)]
        public short Result;
    }

    [MessagePackObject]
    public class PKTNtfRoomLeaveUser
    {
        [Key(0)]
        public string UserID;
    }


    [MessagePackObject]
    public class PKTReqRoomChat
    {
        [Key(0)]
        public string ChatMessage;
    }

    [MessagePackObject]
    public class PKTNtfRoomChat
    {
        [Key(0)]
        public string UserID;

        [Key(1)]
        public string ChatMessage;
    }
}
