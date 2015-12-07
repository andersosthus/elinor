namespace Elinor.Models
{
    internal class CharWrapper
    {
        internal long CharId { get; set; }
        internal int KeyId { get; set; }
        internal string Charname { get; set; }
        internal string VCode { get; set; }

        public override string ToString()
        {
            return Charname;
        }
    }
}