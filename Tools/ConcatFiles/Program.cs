using System.IO;

namespace ConcatFiles
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var writer = File.OpenWrite(args[0]))
            {
                for (var i = 1; i < args.Length; i++)
                {
                    using (var reader = File.OpenRead(args[i]))
                        reader.CopyTo(writer);
                }
            }
        }
    }
}
