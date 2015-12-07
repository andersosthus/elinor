using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Elinor
{
    internal class VolumeFetcher
    {
        internal static Dictionary<string, long> GetVolumes(List<List<string>> table)
        {
            long sellvol = 0, buyvol = 0;
            long sellinit = 0, buyinit = 0;
            var result = new Dictionary<string, long>();

            var inrange = from List<string> row in table
                where row[13] == "0"
                select row;

            foreach (var v in inrange)
            {
                long l;
                double d;
                switch (v[7])
                {
                    case "False":
                        if (double.TryParse(v[1], NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                            sellvol += (long) d;
                        if (long.TryParse(v[5], NumberStyles.Any, CultureInfo.InvariantCulture, out l))
                            sellinit += l;
                        break;
                    case "True":
                        if (double.TryParse(v[1], NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                            buyvol += (long) d;
                        if (long.TryParse(v[5], NumberStyles.Any, CultureInfo.InvariantCulture, out l))
                            buyinit += l;
                        break;
                }
            }

            long selldiff = sellinit - sellvol;
            long buydiff = buyinit - buyvol;

            result.Add("sellvol", sellvol);
            result.Add("sellmov", selldiff);
            result.Add("buyvol", buyvol);
            result.Add("buymov", buydiff);

            return result;
        }
    }
}