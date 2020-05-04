using System;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;
using General;

namespace DJCS.AzurePlayground.ConsoleCosmosStorage
{
    class Program
    {
        private static IConfigurationRoot _configuration;

        static async Task Main(string[] args)
        {

            //Acedemos ao ficheiro JSON de configurações
            _configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", false)
                                                          .Build();

            await TrabalharComOCosmos();

            // await TrabalharComOAzureSQL();

            Console.ReadLine();
        }

        private static async Task TrabalharComOAzureSQL()
        {
            using (var conexao = new SqlConnection(_configuration.GetConnectionString("AzureSQL")))
            {
                var sql = "INSERT INTO Sessoes (Titulo, Apresentador, Host, DataSessao) VALUES ('Automação de tarefas com Lambda e Phyton', 'Mário Pinho', 'Martins Isata', '20200516')";
                var sqlCommand = new SqlCommand(sql, conexao);
                sqlCommand.Connection.Open();
                sqlCommand.ExecuteNonQuery();
                sqlCommand.Connection.Close();


                var sql2 = "SELECT * FROM Sessoes";
                var sqlCommand2 = new SqlCommand(sql2, conexao);
                sqlCommand2.Connection.Open();
                var leitor = sqlCommand2.ExecuteReader();

                while (await leitor.ReadAsync())
                {
                    Console.WriteLine("Sessão Inserida com o título '" + leitor["Titulo"]);
                }

                sqlCommand2.Connection.Close();
            }
        }

        private static async Task TrabalharComOCosmos()
        {
            //Obtemos os valores do Caminho de Conexão ao servidor
            //e a chave primária a ser utilizada
            var connectionString = _configuration.GetConnectionString("CosmosDB");
            var chavePrimaria = _configuration["CosmosChavePrimaria"].ToString();

            Console.WriteLine("Hello World!");

            //Instanciar um cliente Cosmos
            var cliente = new CosmosClient(connectionString, chavePrimaria);
            var demoBd = cliente.GetDatabase("TechTalkDb");
            var demoContentor = demoBd.GetContainer("Sessions");

            await ConsultaRegistosNaBDAsync(demoContentor);

            var novaSessao = await InsereRegistoNaBdAsync(demoContentor);
            await ConsultaRegistosNaBDAsync(demoContentor);

            await ActualizaRegistoNaBdAsync(demoContentor, novaSessao.Resource);
            await ConsultaRegistosNaBDAsync(demoContentor);


            //Criar objectos se quisessemos
            // var bd = await cliente.CreateDatabaseIfNotExistsAsync("MinhaNovaDB");
            // var contentor = bd.Database.CreateContainerIfNotExistsAsync("MeuNovoContentor", "/MinhaChaveDePartição");
        }

        private static async Task ActualizaRegistoNaBdAsync(Container demoContentor, Sessao sessao)
        {
            var consultaSQL = $"SELECT * FROM c WHERE c.AreaConhecimento = '{ sessao.AreaConhecimento }'";
            var definidorConsulta = new QueryDefinition(consultaSQL);
            var itemLido = await demoContentor.ReadItemAsync<Sessao>(sessao.Id.ToString(), new PartitionKey(sessao.AreaConhecimento));

            var sessaoPorActualizar = itemLido.Resource;
            sessaoPorActualizar.Participantes = new Participante[] {
                new Participante() { Nome = "Martins Isata"}
            };

            await demoContentor.ReplaceItemAsync(sessaoPorActualizar, sessaoPorActualizar.Id.ToString(), new PartitionKey(sessaoPorActualizar.AreaConhecimento));
        }

        private  static Task<ItemResponse<Sessao>> InsereRegistoNaBdAsync(Container demoContentor)
        {
            var sessao = new Sessao()
            {
                Id = Guid.NewGuid().ToString(),
                Titulo = "Armazenamento de dados com o Microsoft Azure - visão geral",
                Apresentador = new Apresentador() { Nome = "Damásio Sabino", Profissao = "Enginheiro de Software" },
                AreaConhecimento = "Azure",
                Host = "Newton Costa",
                DataSessao = DateTime.Now,
            };

            return demoContentor.CreateItemAsync<Sessao>(sessao);
        }

        private static async Task ConsultaRegistosNaBDAsync(Container demoContentor)
        {
            var consultaSQL = "SELECT * FROM c";
            var definidorConsulta = new QueryDefinition(consultaSQL);
            var iteradorConsulta = demoContentor.GetItemQueryIterator<Sessao>(definidorConsulta);

            while (iteradorConsulta.HasMoreResults)
            {
                var documentos = await iteradorConsulta.ReadNextAsync();

                foreach (var documento in documentos)
                {
                    Console.WriteLine($"A sessão entitulada { documento.Titulo } apresentada por { documento.Apresentador }, participaram:");

                    if (documento.Participantes == null)
                    {
                        continue;
                    }

                    foreach (var participante in documento.Participantes)
                    {
                        Console.WriteLine($"{ participante.Nome };");
                    }
                }

            }
        }
    }
}
