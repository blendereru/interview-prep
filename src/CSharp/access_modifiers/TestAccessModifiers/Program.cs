using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("AccessModifiers")]
namespace TestAccessModifiers
{
    public class Program
    {
        public static void Main(string[] args)
        {
            First f = new First();
            //f.PrivateMember(); -> impossible, as PrivateMember is private
            
            Second s = new Second();
            //s.ProtectedMember(); -> impossible, as ProtectedMember is protected
            s.InternalMember(); // -> is safe, as accessed from the same assembly
            
            Third t = new Third();
            //t.ProtectedMember(); // -> impossible as ProtectedMember is accessible to derived classes but private for accessing

            Fourth fourth = new Fourth();
            fourth.ProtectedInternalMember(); // safe as member getting accessed from the same assembly
        }
    }

    internal class First
    {
        public void Member()
        {
            PrivateMember(); // safe as PrivateMember is getting accessed within the same class
        }

        private void PrivateMember()
        {
            
        }
    }
    
    public class Second
    {
        public void Member()
        {
            
        }

        internal void InternalMember()
        {
            
        }
        
        protected void ProtectedMember()
        {
            
        }
    }

    public class Third : Second
    {

        public void Test()
        {
            var second = new Second();
            // second.ProtectedMember();  error, not accessed through the derived class type
            Third third = new Third();
            third.ProtectedMember(); // safe, as accessed through the derived class type.
        }
        public void Method()
        {
            ProtectedMember(); // safe as ProtectedMember is accessible to derived classes but private for accessing
        }
    }

    public class Fourth
    {
        protected internal virtual int ProtectedInternalMember()
        {
            return 0;
        }
    }

    public class Sixth
    {
        private protected virtual int PrivateProtectedMember()
        {
            Fourth fourth = new Fourth();
            return fourth.ProtectedInternalMember();
        }
    }

    public class Seventh : Sixth
    {
        private protected override int PrivateProtectedMember()
        {
            // Sixth sixth = new Sixth();
            // sixth.PrivateProtectedMember(); is not safe as this member can only be accessed by classes derived from Sixth
            Seventh seventh = new Seventh();
            seventh.PrivateProtectedMember(); // safe, all conditions followed
            return 3;
        }
    }

    public class Eleventh : Fourth
    {
        protected internal override int ProtectedInternalMember()
        {
            return base.ProtectedInternalMember();
        }
    }
}