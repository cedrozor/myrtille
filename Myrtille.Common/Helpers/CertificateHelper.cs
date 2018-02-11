/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2018 Cedric Coste

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

        http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
*/

using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using CERTENROLLLib;

namespace Myrtille.Helpers
{
    public static class CertificateHelper
    {
        public static X509Certificate2 CreateSelfSignedCertificate(string issuer, string name)
        {
            // CAUTION! this method fails under "vanilla" Windows 2008 and requires an adaptation for a Windows 10 build
            // see http://stackoverflow.com/questions/33001983/issues-compiling-in-windows-10/35365099#35365099

            try
            {
                // create a DN for issuer and subject
                var dn = new CX500DistinguishedName();
                dn.Encode("CN=" + issuer, X500NameFlags.XCN_CERT_NAME_STR_NONE);

                // create a private key for the certificate
                var privateKey = (IX509PrivateKey)Activator.CreateInstance(Type.GetTypeFromProgID("X509Enrollment.CX509PrivateKey"));
                privateKey.ProviderName = "Microsoft Base Cryptographic Provider v1.0";
                privateKey.MachineContext = true;
                privateKey.Length = 2048;
                privateKey.KeySpec = X509KeySpec.XCN_AT_SIGNATURE;
                privateKey.ExportPolicy = X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_PLAINTEXT_EXPORT_FLAG;
                privateKey.Create();

                // use the stronger SHA512 hashing algorithm
                var hashobj = new CObjectId();
                hashobj.InitializeFromAlgorithmName(ObjectIdGroupId.XCN_CRYPT_HASH_ALG_OID_GROUP_ID, ObjectIdPublicKeyFlags.XCN_CRYPT_OID_INFO_PUBKEY_ANY, AlgorithmFlags.AlgorithmFlagsNone, "SHA512");

                // add extended key usage (look at MSDN for a list of possible OIDs)
                var oid = new CObjectId();
                oid.InitializeFromValue("1.3.6.1.5.5.7.3.1");   // SSL server
                var oidlist = new CObjectIds();
                oidlist.Add(oid);
                var eku = new CX509ExtensionEnhancedKeyUsage();
                eku.InitializeEncode(oidlist);

                // create the self signing request
                var cert = (IX509CertificateRequestCertificate)Activator.CreateInstance(Type.GetTypeFromProgID("X509Enrollment.CX509CertificateRequestCertificate"));
                // Windows 2008 R2 and above
                cert.InitializeFromPrivateKey(X509CertificateEnrollmentContext.ContextMachine, (CX509PrivateKey)privateKey, "");
                // Windows 10
                //cert.InitializeFromPrivateKey(X509CertificateEnrollmentContext.ContextMachine, privateKey, "");
                cert.Issuer = dn;
                cert.Subject = dn;
                cert.NotBefore = DateTime.Now;
                cert.NotAfter = DateTime.Now.AddYears(1);       // 1 year expiration
                cert.X509Extensions.Add((CX509Extension)eku);
                cert.HashAlgorithm = hashobj;
                cert.Encode();

                // do the final enrollment process
                var enroll = (IX509Enrollment)Activator.CreateInstance(Type.GetTypeFromProgID("X509Enrollment.CX509Enrollment"));
                enroll.InitializeFromRequest(cert);
                enroll.CertificateFriendlyName = name;
                string csr = enroll.CreateRequest();

                // output a base64 encoded PKCS#12
                enroll.InstallResponse(InstallResponseRestrictionFlags.AllowUntrustedCertificate, csr, EncodingType.XCN_CRYPT_STRING_BASE64, "");
                var base64encoded = enroll.CreatePFX("", PFXExportOptions.PFXExportChainWithRoot);

                // return the certificate
                return new X509Certificate2(Convert.FromBase64String(base64encoded), "", X509KeyStorageFlags.Exportable);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to create a self-signed certificate ({0})", exc);
                throw;
            }
        }
    }
}