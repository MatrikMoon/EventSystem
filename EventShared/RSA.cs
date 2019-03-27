using Google.Protobuf;
using System;
using System.IO;
using System.Security.Cryptography;
using static TeamSaberShared.SharedConstructs;

/*
 * Created by Moon on 9/9/2018
 * Handles RSA signature generation of communications
 */

namespace TeamSaberShared
{
    public class RSA
    {
        private static string privKey = "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
            "<RSAParameters xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" +
                "<Exponent>AQAB</Exponent>" +
                "<Modulus>x23auv3xlc95P9QBuyJHt89zZfiSMLscQ0bhLx4eJ0lJoDPD6OKP/iy+qTHFyinTg/IFAC664oskB8w22P1wo3Wm/BQk412rSrtZju50t/TvxxD5pQ9bYpcK3rvH8reS6bNgTxkGSIW4hrPUkxlybY0/Xsoe2fIDB+dCEnLHWzbnSGIjJ/d3IfBls+l86phEMIBtEC9WjAS8GO49VHV0fj1RyqH8iPrx9yZFmSZekF5mDy53yYXvRGKGYVolGQUZ5PEJdJF2Xt8eawCQDaqAQljuv3qCV+MkkRRP9LcSx/g7Kw5sAC7KKdCl+AOF7UJ4u30QWN01++E7LaLMsne11Q==</Modulus>" +
                "<P>zywp4AiehHFFloHwgzWfoVsqvhJMgj5sf9EsrP++5Zsw6EyDbvok62xgTVzDzDEQhJS/oHgzaAktp9dmkuAqPae//sVRFZ1Q1c7r7NIaJIbMIORPJ0mwKMZFbsQk7WoUweeYkFDoWMI0h1du3GhQwsc2xYYhg3GhtAScuTlOaF8=</P>" +
                "<Q>9m59cKKmsMm8cDboxfe78G10cOsZVRlMB5uIIudvsGnASfvy1A+FXGYZ3xzvmV10m/VQmKPaRCm5bRF8yxjf/MRYTi4nw2qPi9jb2BFFlVKTDLMT9DacCcjaMT40M9RGOkqDUmAqJukL48idJhs+/PbjntXsoO3fmj7c/f48Hks=</Q>" +
                "<DP>yZ/rqTP6Ql+TICWaE7iOgRUfBhj9CQ0Dv2muFzhXa1KcSJiemdUtNUomd2Q+0m017DJwRRZ2wVudaWoDVBKCSbsG6kbS4TxXvZ5CkhrwgngGFkTcnBnlLem5DVIrtju1s/lXy6xSVH+9a7K2HCAR6V7EeXxPBYQDohWTCdkx6/U=</DP>" +
                "<DQ>7B/6ug5fwk3K7Yrvh6FUx49ZX0klwNC4dSGmVCuGbXcm9L0hc+hbVKnQaGSFgGJ39WgdjeSSo3WHYua6uLBhDwXjxyWanDhiyxFDtcj275lrpWOB3yLkaMu3pi+APZlMoVX8dtYGDbqH1f7H9VduB11ZxTwdvCWxd0582jLcz/k=</DQ>" +
                "<InverseQ>bDz4ymgsNKXfA4eWw2rQhy5FO2wtzfcrHJxReRWpaPzYeNjLfhoS9Hc/xCeu0wHFVj3gcpkDyCPz3vw3UOIaTceqOgpTqWTo1ztGX4gG6j8Ri/dH79usxMSMYJ6+X3UjB2Uew42GHF/CZmlhyPitko73kFczTTQ29aciweNq3bc=</InverseQ>" +
                "<D>d4nGiT08ONMS14qJuxTSLkBf6Gh1oFYXm8/5nFeONXg9dJRywr5DF1TUt/AIoaQuj7FbA//+r1o9r5rd6XUItkIIZKLidZKo4POpu/384mMj8oufSCwLV+43asjeEgOtQP3vJZpXZNMah0t1iPLA3x/CM1wt3++rl2or9YjtLCON22fXUT08OWL6db0BdlXSjme54GL9J2jiTbVifRT0t3pEjK8O0lyUj8TEjlBJJ62HeUJmyW4DrY/ztQRlWSr0Fqi7XRpW/VVV6iK5zNDjWvsRp6lXULS+vSNg74MWC6nAnFIsscnAcVqcmuVW1N12+xLRColIAhaRmM4lfPh8xQ==</D>" +
            "</RSAParameters>";

        private static string pubKey = "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
            "<RSAParameters xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" +
                "<Exponent>AQAB</Exponent>" +
                "<Modulus>x23auv3xlc95P9QBuyJHt89zZfiSMLscQ0bhLx4eJ0lJoDPD6OKP/iy+qTHFyinTg/IFAC664oskB8w22P1wo3Wm/BQk412rSrtZju50t/TvxxD5pQ9bYpcK3rvH8reS6bNgTxkGSIW4hrPUkxlybY0/Xsoe2fIDB+dCEnLHWzbnSGIjJ/d3IfBls+l86phEMIBtEC9WjAS8GO49VHV0fj1RyqH8iPrx9yZFmSZekF5mDy53yYXvRGKGYVolGQUZ5PEJdJF2Xt8eawCQDaqAQljuv3qCV+MkkRRP9LcSx/g7Kw5sAC7KKdCl+AOF7UJ4u30QWN01++E7LaLMsne11Q==</Modulus>" +
            "</RSAParameters>";

        public static string SignScore(ulong userId, string songId, int difficultyLevel, bool fullCombo, int score, int playerOptions, int gameOptions)
        {
            var sr = new StringReader(pubKey);
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            var pubkey = (RSAParameters)xs.Deserialize(sr);

            sr = new StringReader(privKey);
            var privkey = (RSAParameters)xs.Deserialize(sr);

            var csp = new RSACryptoServiceProvider();
            csp.ImportParameters(privkey);

            var plainTextData = userId + songId + difficultyLevel + fullCombo + score + playerOptions + gameOptions + "<3";
            var bytesPlainTextData = System.Text.Encoding.Unicode.GetBytes(plainTextData);

            var bytesSignedText = csp.SignData(bytesPlainTextData, CryptoConfig.MapNameToOID("SHA512"));
            var signedText = Convert.ToBase64String(bytesSignedText);

            return signedText;
        }
    }
}
