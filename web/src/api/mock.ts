import type { Article } from '../types/article'

const daysAgo = (n: number) => new Date(Date.now() - n * 86_400_000).toISOString()

export const mockArticles: Article[] = [
  {
    id: '3f1c7a10-0000-4000-8000-000000000001',
    titulo: 'Análise de Algoritmos de Ordenação em Ambientes Distribuídos',
    autores: 'Ana Beatriz Souza, Carlos Eduardo Lima',
    resumo:
      'Este trabalho compara o desempenho de algoritmos de ordenação clássicos quando executados em clusters com diferentes níveis de latência de rede.\n\nOs experimentos foram conduzidos em instâncias EC2 e mostram que o merge sort distribuído escala melhor que o quick sort para grandes volumes de dados.',
    palavrasChave: 'algoritmos, ordenação, sistemas distribuídos, AWS',
    area: 'Computação',
    pdfS3Key: 'articles/3f1c7a10/ordenacao.pdf',
    resumoTraduzido:
      'This work compares the performance of classic sorting algorithms running on clusters with different network latencies.\n\nExperiments on EC2 instances show that distributed merge sort scales better than quick sort for large datasets.',
    statusProcessamento: 'concluido',
    criadoEm: daysAgo(12),
    atualizadoEm: daysAgo(10),
  },
  {
    id: '3f1c7a10-0000-4000-8000-000000000002',
    titulo: 'Impacto da Atividade Física na Saúde Mental de Universitários',
    autores: 'Beatriz Alves Pereira',
    resumo:
      'Estudo transversal com 300 estudantes de graduação que avalia a relação entre a prática regular de exercícios e os níveis de ansiedade e depressão.\n\nOs resultados indicam redução significativa dos sintomas entre praticantes regulares.',
    palavrasChave: 'saúde mental, exercício físico, universitários, ansiedade',
    area: 'Saúde',
    pdfS3Key: 'articles/3f1c7a10/saude-mental.pdf',
    resumoTraduzido: null,
    statusProcessamento: 'processando',
    criadoEm: daysAgo(5),
    atualizadoEm: null,
  },
  {
    id: '3f1c7a10-0000-4000-8000-000000000003',
    titulo: 'Otimização Estrutural de Pontes em Concreto Protendido',
    autores: 'Diego Ramos Ferreira, Fernanda Costa Nogueira',
    resumo:
      'Apresenta-se um modelo de otimização para o dimensionamento de vigas de pontes rodoviárias em concreto protendido.\n\nO método reduziu em média 14% o consumo de aço sem comprometer os critérios de segurança da norma vigente.',
    palavrasChave: 'estruturas, concreto protendido, otimização, pontes',
    area: 'Engenharia',
    pdfS3Key: 'articles/3f1c7a10/pontes.pdf',
    resumoTraduzido: null,
    statusProcessamento: 'pendente',
    criadoEm: daysAgo(2),
    atualizadoEm: null,
  },
  {
    id: '3f1c7a10-0000-4000-8000-000000000004',
    titulo: 'Letramento Digital em Comunidades Ribeirinhas da Amazônia',
    autores: 'Gabriela Nunes Barbosa, Heitor Menezes',
    resumo:
      'Pesquisa qualitativa sobre o acesso e o uso de tecnologias digitais em comunidades ribeirinhas do Amazonas.\n\nAs entrevistas revelam barreiras de infraestrutura e de formação, além de estratégias comunitárias de aprendizagem.',
    palavrasChave: 'educação, inclusão digital, Amazônia',
    area: 'Humanas',
    pdfS3Key: 'articles/3f1c7a10/letramento.pdf',
    resumoTraduzido: null,
    statusProcessamento: 'erro',
    criadoEm: daysAgo(20),
    atualizadoEm: daysAgo(19),
  },
  {
    id: '3f1c7a10-0000-4000-8000-000000000005',
    titulo: 'Métodos Numéricos para Equações Diferenciais Parciais Não Lineares',
    autores: 'Isabela Martins Rocha',
    resumo:
      'Revisão e comparação de métodos de diferenças finitas e elementos finitos aplicados a equações diferenciais parciais não lineares.\n\nSão discutidos estabilidade, convergência e custo computacional.',
    palavrasChave: 'métodos numéricos, EDP, elementos finitos',
    area: 'Exatas',
    pdfS3Key: 'articles/3f1c7a10/edp.pdf',
    resumoTraduzido:
      'Review and comparison of finite difference and finite element methods applied to nonlinear partial differential equations.\n\nStability, convergence and computational cost are discussed.',
    statusProcessamento: 'concluido',
    criadoEm: daysAgo(30),
    atualizadoEm: null,
  },
]
