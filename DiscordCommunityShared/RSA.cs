using Google.Protobuf;
using System;
using System.IO;
using System.Security.Cryptography;

/*
 * Created by Moon on 9/9/2018
 * Handles RSA signature generation of communications
 */

namespace DiscordCommunityShared
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

        /*
        static void GenerateKeyPair()
        {
            //lets take a new CSP with a new 2048 bit rsa key pair
            var csp = new RSACryptoServiceProvider(2048);

            var privkey = csp.ExportParameters(true);
            var pubkey = csp.ExportParameters(false);

            var sw = new StringWriter();
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            xs.Serialize(sw, pubkey);
            pubKey = sw.ToString();

            sw = new StringWriter();
            xs.Serialize(sw, privkey);
            privKey = sw.ToString();

            Logger.Warning("PUBLIC KEY: " + pubKey);
            Logger.Warning("PRIVATE KEY: " + privKey);
        }
        */

        public static string SignScore(ulong userId, string songId, int difficultyLevel, int gameplayMode, int score)
        {
            /*
            if (privKey == null || pubKey == null)
            {
                GenerateKeyPair();
            }
            */

            var sr = new StringReader(pubKey);
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            var pubkey = (RSAParameters)xs.Deserialize(sr);

            sr = new StringReader(privKey);
            var privkey = (RSAParameters)xs.Deserialize(sr);

            var csp = new RSACryptoServiceProvider();
            csp.ImportParameters(privkey);

            var plainTextData = userId + songId + difficultyLevel + gameplayMode + score + "<3";
            var bytesPlainTextData = System.Text.Encoding.Unicode.GetBytes(plainTextData);

            var bytesSignedText = csp.SignData(bytesPlainTextData, CryptoConfig.MapNameToOID("SHA512"));
            var signedText = Convert.ToBase64String(bytesSignedText);

            return signedText;
            /*
            //first, get our bytes back from the base64 string ...
            bytesCypherText = Convert.FromBase64String(cypherText);

            //we want to decrypt, therefore we need a csp and load our private key
            csp = new RSACryptoServiceProvider();
            csp.ImportParameters(privKey);

            //decrypt and strip pkcs#1.5 padding
            bytesPlainTextData = csp.Decrypt(bytesCypherText, false);

            //get our original plainText back...
            plainTextData = System.Text.Encoding.Unicode.GetString(bytesPlainTextData);
            */
        }
    }
}
