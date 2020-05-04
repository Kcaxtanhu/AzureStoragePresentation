using System;
using Newtonsoft.Json;

namespace General
{
    public class Sessao
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string Titulo { get; set; }
        public Apresentador Apresentador { get; set; }
        public string Host { get; set; }
        public DateTime DataSessao { get; set; }
        public string AreaConhecimento { get; set; }
        public Participante[] Participantes { get; set; }
    }

    public class Apresentador
    {
        public string Nome { get; set; }
        public string Profissao { get; set; }
    }

    public class Participante
    {
        public string Nome { get; set; }
        public string Email { get; set; }
    }
}