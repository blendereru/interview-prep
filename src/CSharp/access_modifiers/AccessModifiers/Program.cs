using TestAccessModifiers;

namespace AccessModifiers
{
    class Program
    {
        public static void Main(string[] args)
        {
            //TestAccessModifiers.First -> not accessible because internal
            TestAccessModifiers.Second s = new TestAccessModifiers.Second();
            s.Member(); // safe because Second is public
            // s.InternalMember(); -> not safe, as different assembly
            
            TestAccessModifiers.Second s2 = new TestAccessModifiers.Second();
            // s2.InternalMember(); -> not accessible because InternalMember is internal
            Fifth f = new Fifth();
            // f.ProtectedInternalMember() -> can't call as member is protected

            Fourth fourth = new Fourth();
            // fourth.ProtectedInternalMember() -> impossible, access to different assembly
        }
    }

    public class Fifth : Fourth
    {
        protected override int ProtectedInternalMember()
        {
            return 1;
        }
        // safe, other assembly can derive and access this member.
    }

    public class Eight : Sixth
    {
        //private protected override int PrivateProtectedMember()
        //{
        //    return 2;
        //}
        // impossible as the method is accessible only within the same class and within the class deriving from the same assembly
    }

    public class Ninth : Second
    {
        public void Test()
        {
            var ninth = new Ninth();
            ninth.ProtectedMember(); // can be accessed from any assembly
        }
    }

    public class Tenth : Fourth
    {
        public void Test()
        {
            Tenth tenth = new Tenth();
            tenth.ProtectedInternalMember(); // safe, derived class can access through a variable of the derived type
        }
    }

    public class Twelve : Sixth
    {
        public void Test()
        {
            PrivateProtectedMember();
        }
    }
}