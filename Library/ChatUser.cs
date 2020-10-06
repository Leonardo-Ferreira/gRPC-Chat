using System;
using Grpc.Core;

namespace Library
{
    public class ChatUser
    {
        public ChatUser(IAsyncStreamReader<ChatMessage> userInputs, IServerStreamWriter<ChatMessage> userChat, string userName)
        {
            UserInputedMessagesStream = userInputs;
            UserChatMessagesStream = userChat;
            UserName = userName;
        }

        /// <summary>
        /// Represents the messages the user typed and sent over
        /// </summary>
        public IAsyncStreamReader<ChatMessage> UserInputedMessagesStream { get; internal set; }

        /// <summary>
        /// Post here messages of other users
        /// </summary>
        public IServerStreamWriter<ChatMessage> UserChatMessagesStream { get; internal set; }

        public string UserName { get; set; }
    }
}
