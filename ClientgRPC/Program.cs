using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Library;

namespace ChatClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            // The port number(5001) must match the port of the gRPC server.
            using var channel = GrpcChannel.ForAddress("http://localhost:5001", new GrpcChannelOptions() { Credentials = ChannelCredentials.Insecure });
            var client = new Messenger.MessengerClient(channel);

            Console.WriteLine("USERNAME:");
            var userName = Console.ReadLine();

            Console.WriteLine("Starting Up, please wait");

            var reply = client.ListRooms(new ListRoomsRequest());

            int attempts = 1;

            var msg = reply.ResponseStream.MoveNext();

            var rooms = new List<AvailableRoom>();

            while (!msg.IsCompleted)
            {
                Console.Clear();
                Console.Write("Starting Up, please wait");
                if (attempts % 2 == 0)
                {
                    Console.WriteLine("...");
                }
                else
                {
                    Console.WriteLine();
                }
                attempts++;
                await Task.Delay(1000);
                if (msg.IsCompleted)
                {
                    if (!msg.Result)
                    {
                        //Finished
                        Console.Clear();
                        Console.WriteLine("Starting Up done!");
                        break;
                    }
                    rooms.Add(reply.ResponseStream.Current);
                    msg = reply.ResponseStream.MoveNext();
                }
            }
            if (rooms.Count == 0)
            {
                Console.WriteLine("No Chat Rooms Yet! Type the new room name and press enter:");
            }
            else
            {
                Console.WriteLine("Please Pick a Room or Type the new room name and press enter:");
                for (int i = 0; i < rooms.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {rooms[i].RoomName}");
                }
            }
            var choice = Console.ReadLine();
            int roomChoosen = 0;
            if (!int.TryParse(choice, out roomChoosen))
            {
                roomChoosen = (await client.CreateRoomAsync(new CreateChatRoomRequest() { RoomName = choice })).RoomId;
            }

            Console.Clear();
            Console.Write(">");

            var chatRoom = client.EnterChatRoom();
            await InitializeChannel(userName, roomChoosen, chatRoom);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () =>
            {
                while (await chatRoom.ResponseStream.MoveNext())
                {
                    MessageReceived(chatRoom.ResponseStream.Current);
                }
            });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            while (true)
            {
                var txt = Console.ReadLine();
                ChatMessage newMsg = new ChatMessage();
                newMsg.Author = userName;
                newMsg.Text = txt;
                newMsg.SystemMessage = false;
                newMsg.RoomId = roomChoosen;
                newMsg.Timestamp = DateTime.Now.ToString();
                chatRoom.RequestStream.WriteAsync(newMsg);
                Console.Write(">");
            }
        }

        private static async Task InitializeChannel(string userName, int roomChoosen, AsyncDuplexStreamingCall<ChatMessage, ChatMessage> chatRoom)
        {
            ChatMessage newMsg = new ChatMessage();
            newMsg.Author = userName;
            newMsg.Text = "";
            newMsg.SystemMessage = true;
            newMsg.RoomId = roomChoosen;
            await chatRoom.RequestStream.WriteAsync(newMsg);
        }

        static void MessageReceived(ChatMessage message)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine($"[{message.Author} @ {message.Timestamp}]: {message.Text}");
            Console.Write(">");
        }
    }
}