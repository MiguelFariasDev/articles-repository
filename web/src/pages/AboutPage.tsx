const SERVICES: [string, string][] = [
  ['EC2', 'hospedagem da aplicação (Auto Scaling Group atrás de um ALB)'],
  ['S3', 'armazenamento dos PDFs'],
  ['RDS', 'PostgreSQL com os dados dos artigos'],
  ['ElastiCache', 'Redis para cache das buscas por área'],
  ['DynamoDB', 'log das ações CRUD'],
  ['SNS + SQS', 'processamento assíncrono: o Worker extrai o texto do PDF e traduz o resumo para inglês'],
]

const STACK = ['React + TypeScript + Vite', 'TailwindCSS', '.NET 10 / ASP.NET Core Web API', 'EF Core + PostgreSQL', 'Worker Service (BackgroundService)', 'Docker Compose + LocalStack']

export default function AboutPage() {
  return (
    <div className="max-w-3xl space-y-4">
      <h1 className="text-2xl font-bold">Sobre o projeto</h1>
      <p>
        O Repositório de Artigos Científicos/TCCs é um trabalho acadêmico que demonstra uma aplicação distribuída na AWS.
        Cada artigo possui título, autores, resumo, palavras-chave, área e um PDF.
      </p>
      <h2 className="text-lg font-semibold">Serviços AWS</h2>
      <ul className="list-disc space-y-1 pl-6">
        {SERVICES.map(([name, desc]) => <li key={name}><strong>{name}</strong> — {desc}</li>)}
      </ul>
      <h2 className="text-lg font-semibold">Stack técnica</h2>
      <ul className="list-disc space-y-1 pl-6">{STACK.map((s) => <li key={s}>{s}</li>)}</ul>
    </div>
  )
}
