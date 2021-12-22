using System;
using System.Globalization;
using System.Text.RegularExpressions;
using JenkinsService.Database;

namespace JenkinsService.Jenkins
{
    /// <summary> My local service nuanses </summary>
    public static class MyJenkinsExtension
    {
        private static Regex _reGetDumpFile = new Regex(@"^Fl:\s+(.*?)\.(?<date>[\d\.]+)$", RegexOptions.Multiline);

        /// <summary> Get dump date from description for Dump subtype </summary>
        public static DateTime? GetDumpInformation(DbeJenkinsJob build)
        {
            //Fbpf Db: 208.0.0.135/fbpf
            //Fl: fbpf.2021.12.13.22.30.41
            //dbV: 63 rcV: 63 currV: 64 isRc: false
            //Inst: C:\Builds\Fbpf\63.rc.179

            var mch = _reGetDumpFile.Match(build.BuildDescription);
            try
            {
                if (mch.Success)
                    return DateTime.ParseExact(mch.Groups["date"].Value, "yyyy.MM.dd.HH.mm.ss", CultureInfo.InvariantCulture);
            }
            catch (FormatException e)
            {
                Console.WriteLine(e);
            }

            return null;
        }

        private static Regex _reGetDb = new Regex(@"^\w+\s+Db:\s+(?<db>.*?)$", RegexOptions.Multiline);

        /// <summary> Get db from description for Dump subtype </summary>
        public static string? GetDumpDbInformation(DbeJenkinsJob build)
        {
            var mch = _reGetDb.Match(build.BuildDescription);
            
            if (mch.Success)
                return mch.Groups["db"].Value;

            return null;
        }
    }
}