using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailKit;
using MimeKit;

namespace GmailAuthentication
{
    public static class MimeMessageExtensions
    {
        public static List<string> ExtractEmailAddressesFromMessageBody(this MimeMessage message)
        {
            var emailAddresses = new List<string>();

            if (message.Body is TextPart textPart)
            {
                ExtractEmailAddressesFromTextPart(textPart, emailAddresses);
            }
            else if (message.Body is Multipart multipart)
            {
                foreach (var bodyPart in multipart)
                {
                    if (bodyPart is TextPart part)
                    {
                        ExtractEmailAddressesFromTextPart(part, emailAddresses);
                    }
                }
            }

            return emailAddresses;
        }

        private static void ExtractEmailAddressesFromTextPart(TextPart textPart, List<string> emailAddresses)
        {
            var regex = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var matches = regex.Matches(textPart.Text);

            foreach (Match match in matches)
            {
                var emailAddress = match.Value;
                if (!emailAddresses.Contains(emailAddress))
                {
                    emailAddresses.Add(emailAddress);
                }
            }
        }
    }

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
                    client.Connect("imap.gmail.com", 993, true);
                    client.Authenticate(_email, _password);

                    var inbox = client.Inbox;
                    inbox.Open(FolderAccess.ReadWrite); // Alterado para permitir marcar os emails como lidos

                    var searchQuery = SearchQuery.NotSeen;

                    var folders = client.GetFolders(client.PersonalNamespaces[0]);
                     Console.ResetColor();
                    Console.WriteLine("Pastas disponíveis:");
                    for (int i = 0; i < folders.Count; i++)
                    {
                        Console.WriteLine($"{i + 1} - {folders[i].FullName}");
                    }

                    Console.Write("Digite o número correspondente à pasta que deseja pesquisar: ");
                    if (int.TryParse(Console.ReadLine(), out int selectedFolderIndex) && selectedFolderIndex >= 1 && selectedFolderIndex <= folders.Count)
                    {
                        var selectedFolder = folders[selectedFolderIndex - 1];
                        selectedFolder.Open(FolderAccess.ReadWrite);
                        // Abre a pasta selecionada

                        try
                        {
                            var searchResults = selectedFolder.Search(searchQuery);

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Existem {searchResults.Count} emails não lidos nessa pasta.");
                            Console.ResetColor();

                            string allEmailText = string.Empty; // Variável para armazenar o texto de todos os emails
                            List<string> emailAddresses = new List<string>(); // Declaração da lista de endereços de email

                            for (int i = 0; i < searchResults.Count; i++)
                            {
                                var uniqueId = searchResults[i];
                                var message = selectedFolder.GetMessage(uniqueId);

                                allEmailText += message.TextBody; // Concatena o texto do corpo do email

                                // Extrair endereços de email do corpo do email
                                var addresses = message.ExtractEmailAddressesFromMessageBody();
                                emailAddresses.AddRange(addresses);

                                // Marcar o email como lido
                                selectedFolder.AddFlags(uniqueId, MessageFlags.Seen, true);


                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"{i + 1} de {searchResults.Count} emails analisados");
                                Console.ResetColor();
                            }

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Mensagem: Terminado de coletar emails. Iniciando análise");
                            Console.ResetColor();

                            // Extrair endereços de email da variável com o texto completo
                            var regex = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                            var matches = regex.Matches(allEmailText);

                            foreach (Match match in matches)
                            {
                                var emailAddress = match.Value;
                                if (!emailAddresses.Contains(emailAddress))
                                {
                                    emailAddresses.Add(emailAddress);
                                }
                            }

                            // Remover emails duplicados
                            emailAddresses = emailAddresses.Distinct().ToList();

                            if (emailAddresses.Count > 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"Foram encontrados {emailAddresses.Count} endereços de email");
                                Console.ResetColor();

                                Console.Write("Deseja enviar a lista de emails por email? (Sim/Não): ");
                                var sendEmailResponse = Console.ReadLine();

                                if (sendEmailResponse.Equals("Sim", StringComparison.OrdinalIgnoreCase) || sendEmailResponse.Equals("1"))
                                {   
                                    
                                    Console.Write("Digite o destinatário do email: ");
                                    var recipient = Console.ReadLine();

                                    Console.Write("Digite o assunto do email: ");
                                    var subject = Console.ReadLine();

                                    SendEmailWithList(emailAddresses, recipient, subject);
                                }
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("Nenhum email encontrado");
                                Console.ResetColor();
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

        private void SendEmailWithList(List<string> emailAddresses, string recipient, string subject)
        {
            try
            {
                // Domínios a serem removidos
                var domainsToRemove = new List<string> { "@tropicalnet", "@googlemail" };

                // Verifica e remove os emails com os domínios especificados da lista
                var emailsToRemove = emailAddresses.Where(email => domainsToRemove.Any(domain => email.Contains(domain))).ToList();
                foreach (var email in emailsToRemove)
                {
                    emailAddresses.Remove(email);
                }

                // Verifica se ainda restam emails na lista após a remoção
                if (emailAddresses.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Nenhum email encontrado (exceto os dos domínios especificados).");
                    Console.ResetColor();
                    return;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Seu Nome", _email));
                message.To.Add(new MailboxAddress("Nome do Destinatário", recipient));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.TextBody = string.Join("\n", emailAddresses);

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    client.Connect("smtp.gmail.com", 587, false);
                    client.Authenticate(_email, _password);

                    client.Send(message);
                    client.Disconnect(true);
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Email enviado com sucesso!");
                Console.ResetColor();
                return;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ocorreu uma exceção ao enviar o email: {ex.Message}");
                Console.ResetColor();
            }
        }

    }
}
