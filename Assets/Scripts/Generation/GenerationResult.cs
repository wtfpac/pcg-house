using System.Collections.Generic;

namespace PCG.Generation
{
    // tudo que uma geracao devolve: a planta, se deu certo, e os numeros do processo
    public class GenerationResult
    {
        public int Seed;
        public FloorPlan Plan;          // null se nenhuma tentativa passou nas regras
        public int Attempts;            // quantas plantas foram tentadas
        public double Milliseconds;     // tempo gasto gerando

        // quantas vezes cada regra reprovou, nesta geracao
        public Dictionary<Failure, int> Failures = new Dictionary<Failure, int>();

        public GenerationResult(int seed)
        {
            Seed = seed;
        }

        public bool Success => Plan != null;

        public void Count(Failure failure)
        {
            if (!Failures.ContainsKey(failure))
                Failures[failure] = 0;

            Failures[failure]++;
        }

        // resumo em texto das falhas desta geracao, pro overlay e o log
        public string FailureSummary()
        {
            if (Failures.Count == 0)
                return "nenhuma";

            List<string> parts = new List<string>();

            foreach (KeyValuePair<Failure, int> pair in Failures)
                parts.Add(pair.Key + ":" + pair.Value);

            return string.Join("  ", parts);
        }
    }
}
