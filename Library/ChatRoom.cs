using System;

namespace Library
{
    public class ChatRoom
    {
        public int Id { get; internal set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; internal set; }

        public ChatRoom(int id, string name = null)
        {
            Id = id;
            Name = name;
        }

        public override bool Equals(object obj)
        {
            return (obj as ChatRoom)?.Id == this.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
