namespace Tests.Types
{
    public class RootObjectWithArray
    {
        public string[] Properties { get; set; } = null!;

        public MidLevel[] Mids { get; set; } = null!;
    }
}