using Google.Protobuf;
using System;
using System.IO;
using System.Security.Cryptography;
using static EventShared.SharedConstructs;

/*
 * Created by Moon on 9/9/2018
 * Handles RSA signature generation of communications
 */

namespace EventShared
{
    public class RSA
    {
        ***REMOVED***
            ***REMOVED***
                ***REMOVED***
                ***REMOVED***
                ***REMOVED***
                ***REMOVED***
                ***REMOVED***
                ***REMOVED***
                ***REMOVED***
                ***REMOVED***
            ***REMOVED***

        ***REMOVED***
            ***REMOVED***
                ***REMOVED***
                ***REMOVED***
            ***REMOVED***

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

        public static string SignSabotage(ulong playerId, string teamId, int score)
        {
            var sr = new StringReader(pubKey);
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            var pubkey = (RSAParameters)xs.Deserialize(sr);

            sr = new StringReader(privKey);
            var privkey = (RSAParameters)xs.Deserialize(sr);

            var csp = new RSACryptoServiceProvider();
            csp.ImportParameters(privkey);

            var plainTextData = playerId + teamId + score + "<3";
            var bytesPlainTextData = System.Text.Encoding.Unicode.GetBytes(plainTextData);

            var bytesSignedText = csp.SignData(bytesPlainTextData, CryptoConfig.MapNameToOID("SHA512"));
            var signedText = Convert.ToBase64String(bytesSignedText);

            return signedText;
        }
    }
}
