using System.Collections.Generic;
using Nancy.Security;

namespace NSemble.Core.Models
{
    public class User : IUserIdentity
    {
        public string Id { get; set; }
        public byte[] Password { get; set; }
        public byte[] Salt { get; set; }
        public IEnumerable<string> Claims { get; set; }

        public string UserName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as User;
            if (other == null) return false;

            return Id.Equals(other.Id) && Email.Equals(other.Email);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}