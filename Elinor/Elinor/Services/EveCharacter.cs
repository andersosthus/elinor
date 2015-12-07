using System.Threading.Tasks;
using eZet.EveLib.EveXmlModule;
using eZet.EveLib.EveXmlModule.Models.Character;

namespace Elinor.Services
{
    public class EveCharacter
    {
        private readonly Character _character;

        public EveCharacter(int keyId, string vCode, long characterId)
        {
            _character = EveXml.CreateCharacter(keyId, vCode, characterId);
        }

        public async Task<CharacterSheet> GetCharacterSheetAsync()
        {
            var charSheet = await _character.GetCharacterSheetAsync();
            return charSheet.Result;
        }

        public async Task<StandingsList> GetCharacterStandingsAsync()
        {
            var standings = await _character.GetStandingsAsync();
            return standings.Result;
        }
    }
}