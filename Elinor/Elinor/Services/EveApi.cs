using System.Threading.Tasks;
using eZet.EveLib.EveXmlModule;
using eZet.EveLib.EveXmlModule.Models.Account;

namespace Elinor.Services
{
    public class EveApi
    {
        private readonly ApiKey _api;

        public EveApi(int keyId, string vCode)
        {
            _api = new ApiKey(keyId, vCode).Init();
        }

        public async Task<CharacterList> GetCharactersAsync()
        {
            var charactes = await _api.GetCharacterListAsync();
            return charactes.Result;
        }
    }
}