using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Myrtille.Common.Interfaces;
using OASIS.Integration;
namespace Myrtille.OASISOTPAdapter
{
    public class OTPAdapter : IMultifactorAuthenticationAdapter
    {
        public string ProviderURL { get { return "http://www.oliveinnovations.com"; } }

        public string PromptLabel
        {
            get
            {
                return "OASIS OTP Code";
            }
        }

        public bool Authenticate(string username, string password)
        {
            string apikey = ConfigurationManager.AppSettings["OASISApiKey"];
            string appkey = ConfigurationManager.AppSettings["OASISAppKey"];
            long appID = long.Parse(ConfigurationManager.AppSettings["OASISAppID"]);
            OTPProvider oasis = new OTPProvider(appID, appkey, apikey);

            var state = oasis.RequestAuthorisationState(new OASIS.Integration.Models.RequestAuthorisationState
            {
                Username = username,
                VerificationType = OASIS.Integration.Models.VerificationTypeEnum.LOGIN
            });

            if (state.State == OASIS.Integration.Models.UserAuthenticatorStateEnum.SKIPAUTHENTICATION) return true;

            if (state.State != OASIS.Integration.Models.UserAuthenticatorStateEnum.AUTHENTICATE) return false;

            var result = oasis.VerifyUserOTP(new OASIS.Integration.Models.VerifyUserOTP
            {
                UserID = state.UserID,
                OTPCode = password,
                VerificationType = OASIS.Integration.Models.VerificationTypeEnum.LOGIN
            });
            return result.State == OASIS.Integration.Models.UserAuthenticatorStateEnum.VALID;
        }
    }
}
