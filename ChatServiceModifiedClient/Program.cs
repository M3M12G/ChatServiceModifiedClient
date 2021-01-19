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
            //var rooms = chatRoomManagerClient.GetAllChatRooms(new All { });

            //If any room exists, you can choose one of them
            using(var call = chatRoomManagerClient.GetAllChatRooms(new All { }))
            {
                while (await call.ResponseStream.MoveNext())
                {
                    if (call.ResponseStream.Current == null)
                    {
                        Console.WriteLine("No rooms exist. Please, enter name of new Chat Room: ");
                        var newChatName = Console.ReadLine();
                        var newCr = await chatRoomManagerClient.CreateNewChatRoomAsync(new CreateChatRoom { ChatName = newChatName });
                        Console.WriteLine($"The new chat room is created: {newCr.Id}-{newCr.RoomName}");
                    }
                    var r = call.ResponseStream.Current;
                    Console.WriteLine($"Room {r.Id}-{r.RoomName}");
                }
            }

            Console.WriteLine("Type id of room");
            var roomId = Convert.ToInt32(Console.ReadLine());

            var cr = await chatRoomManagerClient.GetExactChatRoomAsync(new LookupRoom { Id = roomId });
            Console.WriteLine($"You will join to room {cr.Id}-{cr.RoomName}");

            using (var chat = chatRoomManagerClient.JoinToChat())
            {
                _ = Task.Run(async () =>
                {
                    while (await chat.ResponseStream.MoveNext())
                    {
                        var response = chat.ResponseStream.Current;
                        Console.WriteLine($"{response.Username} : {response.Text}");
                    }
                });

                await chat.RequestStream.WriteAsync(new Message { Username = user, Text = $"{user} has joined the chat!", RoomId=roomId });

                string line;

                while ((line = Console.ReadLine()) != null)
                {
                    if (line.ToUpper() == "EXIT")
                    {
                        break;
                    }
                    await chat.RequestStream.WriteAsync(new Message { Username = user, Text = line, RoomId=roomId });
                }

                await chat.RequestStream.CompleteAsync();
            }


            Console.WriteLine("Channel disconnected");
            await channel.ShutdownAsync();
        }
    }
}
