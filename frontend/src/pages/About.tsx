const SERVICES = [
  ['EC2', 'hospedagem da aplicação (Auto Scaling Group atrás de um ALB)'],
  ['S3', 'armazenamento dos PDFs'],
  ['RDS', 'PostgreSQL com os metadados dos artigos'],
  ['ElastiCache', 'Redis para cache das buscas por área'],
  ['DynamoDB', 'log de todas as ações CRUD'],
  ['SNS + SQS', 'processamento assíncrono: o Worker extrai o texto do PDF e traduz o resumo para inglês'],
]

const STACK = ['.NET 10 / ASP.NET Core Web API', 'EF Core + Npgsql', 'Worker Service (BackgroundService)', 'React 19 + TypeScript + Vite', 'Bootstrap 5', 'Docker Compose + LocalStack']

export default function About() {
  return (
    <div style={{ maxWidth: 760 }}>
      <h1 className="h3 mb-3">Sobre o projeto</h1>
      <p>
        O Repositório de Artigos Científicos/TCCs é um trabalho acadêmico que demonstra uma aplicação distribuída na AWS.
        Cada artigo possui título, autores, resumo, palavras-chave, área e um PDF obrigatório.
      </p>
      <h2 className="h5 mt-4">Serviços AWS</h2>
      <ul>{SERVICES.map(([n, d]) => <li key={n}><strong>{n}</strong> — {d}</li>)}</ul>
      <h2 className="h5 mt-4">Stack técnica</h2>
      <ul>{STACK.map((s) => <li key={s}>{s}</li>)}</ul>
    </div>
  )
}
