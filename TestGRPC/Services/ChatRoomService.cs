using System;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Library;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ChatServer.Services
{
    public class ChatRoomService : Messenger.MessengerBase
    {
        ConcurrentDictionary<ChatRoom, List<ChatUser>> _chatRooms;
        public ChatRoomService()
        {
            _chatRooms = new ConcurrentDictionary<ChatRoom, List<ChatUser>>();
        }

        public override async Task EnterChatRoom(IAsyncStreamReader<ChatMessage> requestStream, IServerStreamWriter<ChatMessage> responseStream, ServerCallContext context)
        {
            await requestStream.MoveNext();
            var routerRequest = requestStream.Current;
            var users = _chatRooms.Where(i => i.Key.Id == routerRequest.RoomId).FirstOrDefault().Value;

            ChatUser user = new ChatUser(requestStream, responseStream, routerRequest.Author);
            users.Add(user);

            while (await requestStream.MoveNext())
            {
                var msg = requestStream.Current;
                if (msg.SystemMessage)
                {
                    msg.Text = $"'{msg.Author}' has joined this room";
                }
                for (int i = 0; i < users.Count; i++)
                {
                    var cUser = users[i];
                    await cUser.UserChatMessagesStream.WriteAsync(msg);
                }
            }
        }

        public override async Task<JoinChatResponse> JoinChat(JoinChatRequest request, ServerCallContext context)
        {
            JoinChatResponse resp = new JoinChatResponse();
            var keys = _chatRooms.Keys;
            var id = keys.FirstOrDefault(i => i.Name == request.RoomName)?.Id;
            if (!id.HasValue)
            {
                resp.RoomId = -1;
            }
            else
            {
                resp.RoomId = id.Value;
            }
            return resp;
        }

        public override async Task<JoinChatResponse> CreateRoom(CreateChatRoomRequest request, ServerCallContext context)
        {
            JoinChatResponse resp = new JoinChatResponse();
            var keys = _chatRooms.Keys;
            if (keys.Any(i => i.Name == request.RoomName))
            {
                resp.RoomId = -1;
            }
            else
            {
                var mId = keys.Any() ? keys.Max(i => i.Id) : 0;
                var room = new ChatRoom(mId + 1, request.RoomName);

                if (!_chatRooms.TryAdd(room, new List<ChatUser>()))
                {
                    resp.RoomId = -1;
                }
                else
                {
                    resp.RoomId = room.Id;
                }
            }
            return resp;
        }

        public override async Task ListRooms(ListRoomsRequest request, IServerStreamWriter<AvailableRoom> responseStream, ServerCallContext context)
        {
            if (_chatRooms.IsEmpty)
            {
                //await responseStream.WriteAsync(new AvailableRoom());
                return;
            }
            var keys = _chatRooms.Keys;
            for (int i = 0; i < keys.Count; i++)
            {
                var item = new AvailableRoom();
                var room = keys.ElementAt(i);
                var participants = _chatRooms[room];
                item.RoomName = room.Name;
                item.Participants = String.Join(",", participants.Take(5));

                //send them slowly, theres no need to rush this, after all
                //the concurrent dictionary is subject to locks
                await responseStream.WriteAsync(item);
            }
        }
    }
}
