using System;
using MailKit.Net.Pop3;
using MimeKit;

namespace GmailAuthentication
{
    public class Authentication
    {
        public void Authenticate()
        {
            try
            {
                Console.Write("Digite seu email do Gmail: ");
                string email = Console.ReadLine();

                Console.Write("Digite sua senha do Gmail: ");
                string password = ReadPassword();

                using (var client = new Pop3Client())
                {
                    client.Connect("pop.gmail.com", 995, true);
                    client.Authenticate(email, password);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Conectado com sucesso!");
                    Console.ResetColor();
                    EmailSearch emailSearch = new EmailSearch(email, password);
                    emailSearch.SearchUnreadEmailsInLabel();
                    // Faça algo após a autenticação bem-sucedida
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ocorreu uma exceção: {ex.Message}");
                Console.ResetColor();
            }

            Console.ReadLine();
        }

        // Função para ler a senha sem exibi-la no console
        private static string ReadPassword()
        {
            string password = "";
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
            }
            while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return password;
        }
    }
}
