using System;
using System.Diagnostics;
using System.IO;
using egregore.Extensions;

namespace egregore.IO
{
    internal static class EggFileManager
    {
        public static bool Create(string eggPath)
        {
            if (eggPath != Constants.DefaultEggPath && eggPath.IndexOfAny(Path.GetInvalidFileNameChars()) > -1)
            {
                Console.Error.WriteErrorLine(Strings.InvalidCharactersInPath);
                return false;
            }

            try
            {
                eggPath = Path.GetFullPath(eggPath);
                if (!Path.HasExtension(eggPath))
                    eggPath = Path.ChangeExtension(eggPath, ".egg");
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Console.Error.WriteErrorLine("Invalid egg file path");
                return false;
            }

            try
            {
                var store = new LogStore(eggPath);
                store.Init();
                Console.Out.WriteLine("Created egg file '{0}'", Path.GetFileName(eggPath));
                return true;
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Console.Error.WriteErrorLine("Failed to create egg file at '{0}'", eggPath);
                return false;
            }
        }
    }
}
