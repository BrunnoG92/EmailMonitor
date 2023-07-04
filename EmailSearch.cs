using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MailKit.Net.Imap;
using MailKit;
using MailKit.Search;
using MailKit.Security;
using MimeKit;

namespace GmailAuthentication
{
    public class EmailSearch
    {
        private readonly string _email;
        private readonly string _password;

        public EmailSearch(string email, string password)
        {
            _email = email;
            _password = password;
        }

        public void SearchUnreadEmailsInLabel()
        {
            try
            {
                using (var client = new ImapClient())
                {
                    client.Connect("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect);
                    client.Authenticate(_email, _password);

                    var inbox = client.Inbox;
                    inbox.Open(FolderAccess.ReadOnly);

                    var searchQuery = SearchQuery.Not(SearchQuery.Seen);

                    var folders = client.GetFolders(client.PersonalNamespaces[0]);

                    Console.WriteLine("Pastas disponíveis:");
                    for (int i = 0; i < folders.Count; i++)
                    {
                        Console.WriteLine($"{i + 1} - {folders[i].FullName}");
                    }

                    Console.Write("Digite o número correspondente à pasta que deseja pesquisar: ");
                    if (int.TryParse(Console.ReadLine(), out int selectedFolderIndex) && selectedFolderIndex >= 1 && selectedFolderIndex <= folders.Count)
                    {
                        var selectedFolder = folders[selectedFolderIndex - 1];
                        selectedFolder.Open(FolderAccess.ReadOnly); // Abre a pasta selecionada

                        try
                        {
                            var searchResults = selectedFolder.Search(searchQuery);

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Existem {searchResults.Count} emails não lidos nessa pasta.");
                            Console.ResetColor();

                            var emailAddresses = new List<string>();

                            foreach (var uniqueId in searchResults)
                            {
                                var message = selectedFolder.GetMessage(uniqueId);

                                // Extrair endereços de email do corpo do email
                                var addresses = ExtractEmailAddressesFromMessageBody(message);
                                emailAddresses.AddRange(addresses);
                            }

                            // Remover duplicatas
                            emailAddresses = emailAddresses.Distinct().ToList();

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Foram encontrados {emailAddresses.Count} endereços de email únicos.");
                            Console.ResetColor();

                            Console.WriteLine("Deseja disparar alerta de emails?");
                            Console.WriteLine("1 - Sim");
                            Console.WriteLine("2 - Não");

                            if (int.TryParse(Console.ReadLine(), out int option))
                            {
                                if (option == 1)
                                {
                                    SendEmailAlert(emailAddresses);
                                }
                                else if (option == 2)
                                {
                                    // Encerrar a execução
                                }
                                else
                                {
                                    Console.WriteLine("Opção inválida. Encerrando a execução.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Opção inválida. Encerrando a execução.");
                            }
                        }
                        catch (FolderNotFoundException)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"A pasta '{selectedFolder.FullName}' não foi encontrada.");
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Opção inválida. Encerrando a execução.");
                        Console.ResetColor();
                    }

                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ocorreu uma exceção: {ex.Message}");
                Console.ResetColor();
            }
        }

        private List<string> ExtractEmailAddressesFromMessageBody(MimeMessage message)
        {
            var emailAddresses = new List<string>();

            if (message.Body is TextPart textPart)
            {
                var bodyText = textPart.Text;

                var regex = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b");
                var matches = regex.Matches(bodyText);

                foreach (Match match in matches)
                {
                    var emailAddress = match.Value;
                    if (!emailAddresses.Contains(emailAddress))
                    {
                        emailAddresses.Add(emailAddress);
                    }
                }
            }

            return emailAddresses;
        }

        private void SendEmailAlert(List<string> emailAddresses)
        {
            // Implemente o código para enviar o alerta de email aqui
            Console.WriteLine("Alerta de email disparado!");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.Write("Digite seu email do Gmail: ");
                string email = Console.ReadLine();

                Console.Write("Digite sua senha do Gmail: ");
                string password = ReadPassword();

                var emailSearch = new EmailSearch(email, password);
                emailSearch.SearchUnreadEmailsInLabel();
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
