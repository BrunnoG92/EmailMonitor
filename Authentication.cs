using System;
using System.IO;
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
                string email, password;

                if (File.Exists("acesso.txt"))
                {
                    string[] lines = File.ReadAllLines("acesso.txt");
                    email = lines[0];
                    password = lines[1];
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Arquivo de credenciais encontrado. Tentando login com os dados locais");
                    Console.ResetColor();

                }
                else
                {
                    Console.Write("Email: ");
                    email = Console.ReadLine();

                    Console.Write("Senha: ");
                    password = ReadPassword();
                }

                using (var client = new Pop3Client())
                {
                    try
                    {
                        client.Connect("pop.gmail.com", 995, true);
                        client.Authenticate(email, password);
                    }
                    catch
                    {   Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Falha na autenticação. Informe novamente os dados de login:");
                        Console.ResetColor();

                        Console.Write("Email: ");
                        email = Console.ReadLine();

                        Console.Write("Senha: ");
                        password = ReadPassword();

                        client.Connect("pop.gmail.com", 995, true);
                        client.Authenticate(email, password);
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Conectado com sucesso!");
                    Console.ResetColor();

                    EmailSearch emailSearch = new EmailSearch(email, password);
                    emailSearch.SearchUnreadEmailsInLabel();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Erro: {ex.Message}");
                Console.ResetColor();
            }

            Console.ReadLine();
        }

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