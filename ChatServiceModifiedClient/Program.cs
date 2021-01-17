using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Threading.Tasks;

namespace ChatServiceModifiedClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Welcom to Chat Room Service... Please, enter your name:");
            var user = Console.ReadLine();

            using var channel = GrpcChannel.ForAddress("https://localhost:5001");

            var chatRoomManagerClient = new ChatRoomProtoServices.ChatRoomProtoServicesClient(channel);

            //check if there exists any room
            var rooms = chatRoomManagerClient.GetAllChatRooms(new All { });
            
            if(rooms.ResponseStream.Current == null)
            {
                Console.WriteLine("No rooms exist. Please, enter name of new Chat Room: ");
                var newChatName = Console.ReadLine();
                var newCr = await chatRoomManagerClient.CreateNewChatRoomAsync(new CreateChatRoom { ChatName = newChatName });
                Console.WriteLine($"The new chat room is created: {newCr.Id}-{newCr.RoomName}");
            }

            //If any room exists, you can choose one of them
            using(var call = chatRoomManagerClient.GetAllChatRooms(new All { }))
            {
                while (await call.ResponseStream.MoveNext())
                {
                    var r = call.ResponseStream.Current;
                    Console.WriteLine($"Room {r.Id}-{r.RoomName}");
                }
            }

            Console.WriteLine("Type id of room");
            var roomId = Convert.ToInt32(Console.ReadLine());

            var cr = await chatRoomManagerClient.GetExactChatRoomAsync(new LookupRoom { Id = roomId });
            Console.WriteLine($"Room {cr.Id}-{cr.RoomName}");

            Console.WriteLine("Channel disconnected");
            await channel.ShutdownAsync();
        }
    }
}
