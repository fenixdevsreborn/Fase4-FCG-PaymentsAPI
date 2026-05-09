# PaymentsAPI

API de processamento de pagamento que consome eventos de pedido de compra, executa o processamento de pagamento de forma assíncrona e publica o resultado (aprovado ou rejeitado) como eventos para outros serviços. 

---

## Índice

1. Visão Geral
2. Responsabilidades no Sistema
3. Arquitetura e Tecnologias
4. Fluxos de Evento
5. Endpoints da API
6. Variáveis de Ambiente
7. Execução

   * Local
   * Docker
   * Docker Compose
   * Kubernetes
8. Observações de Qualidade para Avaliação

---

## 1. Visão Geral

O **PaymentsAPI** recebe eventos de pagamento (geralmente originados no serviço de catálogo após pedido de compra), realiza a simulação/execução do pagamento (lógica de regra de negócio, validações e integrações com gateway de pagamento simulados ou reais) e publica um evento de resultado, indicando aprovação ou rejeição. 

Esse serviço é fundamental para completar o ciclo de compra da arquitetura de microsserviços: após o pedido ser criado, o serviço de pagamento processa e notifica outros serviços (como atualizações de inventário, notificações, biblioteca de jogos etc.). 

---

## 2. Responsabilidades no Sistema

| Serviço                   | Responsabilidade                                                                                              |
| ------------------------- | ------------------------------------------------------------------------------------------------------------- |
| **PaymentsAPI**           | Consumir eventos de pagamento pendentes, processar a lógica de pagamento e publicar o resultado do pagamento. |
| **CatalogAPI**            | Enviar eventos de pedido de compra e reagir aos eventos de pagamento aprovado/rejeitado.                      |
| **UsersAPI**              | Fornecer contexto de usuário e dados associados ao pagamento quando necessário.                               |
| **NotificationsAPI**      | Enviar confirmações/alertas aos usuários com base no resultado do pagamento.                                  |
| **AuthService** (externo) | Validar tokens e perfis de usuário.                                                                           |

---

## 3. Arquitetura e Tecnologias

O PaymentsAPI é construído sobre a plataforma **.NET** com foco em mensageria e padrões de microsserviços. 

**Principais Tecnologias:**

* .NET (versão compatível com o restante do ecossistema Fase 2)
* MassTransit para abstração de mensageria
* RabbitMQ para transporte de mensagens assíncronas
* Entity Framework Core + PostgreSQL (assumido como padrão, ajustar se diferente)
* Docker e Kubernetes para containerização e orquestração
* Health checks para broker e banco de dados

---

## 4. Fluxos de Evento

A comunicação com outros microsserviços é majoritariamente orientada a eventos. Os principais fluxos são:

### 4.1. Processamento de Pagamento

1. **Pedido criado:** outro serviço (CatalogAPI) publica um evento de pedido de compra.
2. **Pagamento recebido:** o PaymentsAPI consome o evento.
3. **Aplicação de regras de pagamento:** validação de integridade, simulação de gateway ou chamada externa real.
4. **Resultado publicado:** após lógica de pagamento, publica evento de pagamento aprovado ou rejeitado.
5. **Consumo por outros serviços:** serviços como NotificationsAPI ou CatalogAPI reagem ao evento para completar o fluxo. 

---

## 5. Endpoints da API

Dependendo de implementação, o PaymentsAPI pode expor endpoints REST para suportar monitoramento e ações administrativas. Um conjunto típico inclui:

| Verbo | Endpoint                | Autenticação | Descrição                                                                          |
| ----- | ----------------------- | ------------ | ---------------------------------------------------------------------------------- |
| GET   | `/health`               | Não          | Checks de saúde do serviço (broker, banco, dependências).                          |
| POST  | `/api/payments/process` | Sim/Não*     | Endpoint para acionar manualmente o processamento de pagamento (quando aplicável). |

* Ajustar conforme controllers reais no código. Se o serviço processa exclusivamente por mensageria, os endpoints REST podem ser mínimos. 

---

## 6. Variáveis de Ambiente

As variáveis a seguir devem ser configuradas antes da execução, via ambiente (local), **ConfigMaps** e **Secrets** em Kubernetes:

### ConfigMap — Não sensíveis

* `RABBITMQ_HOST` — Endereço do broker RabbitMQ
* `RABBITMQ_EXCHANGE_PAYMENTS` — Nome da exchange de pagamentos
* `RABBITMQ_QUEUE_PAYMENT_RESULT` — Fila de resultados de pagamento
* `ASPNETCORE_ENVIRONMENT` — Ambiente da aplicação

### Secrets — Sensíveis

* `RABBITMQ_USERNAME` — Usuário do broker
* `RABBITMQ_PASSWORD` — Senha do broker
* `POSTGRES_CONNECTION_STRING` — Conexão com banco de dados
* `PAYMENT_GATEWAY_API_KEY` — Chave de gateway de pagamentos (se habilitado)

> Em produção, **nunca exponha** secrets diretamente em configurações de ambiente sem gerenciamento seguro (ex.: Vault, Kubernetes Secrets).

---

## 7. Execução

### 7.1 Local (Desenvolvimento)

Siga estes passos para executar o serviço localmente:

1. Clone o repositório:

   ```bash
   git clone https://github.com/thefenixdevs/Fase2-PaymentsAPI.git
   ```
2. Posicione variáveis de ambiente adequadas (ex.: `.env`, PowerShell, export).
3. Garanta que RabbitMQ e PostgreSQL estejam disponíveis.
4. Execute com .NET CLI:

   ```bash
   dotnet restore
   dotnet build
   dotnet run --project PaymentsApi/PaymentsApi.csproj
   ```

---

### 7.2 Docker

1. Construa a imagem:

   ```bash
   docker build -t payments-api .
   ```
2. Execute o container com as variáveis:

   ```bash
   docker run -e RABBITMQ_HOST=... -e RABBITMQ_USERNAME=... -e RABBITMQ_PASSWORD=... payments-api
   ```

---

### 7.3 Docker Compose

Se há um arquivo `docker-compose.yaml` no repositório, execute:

```bash
docker compose up --build
```

Ele deve orquestrar PaymentsAPI, RabbitMQ e PostgreSQL para testes locais.

---

### 7.4 Kubernetes

Manifests estão localizados em `/k8s`. Para deploy:

```bash
kubectl apply -f k8s/
```

Certifique­se de criar **Secrets** e **ConfigMaps** antes da aplicação.

---

## 8. Observações de Qualidade para Avaliação

Para fins de **avaliação acadêmica (Fase 2)**, verifique:

* A utilização de **MassTransit** com RabbitMQ para comunicação assíncrona. 
* Separação de responsabilidades entre consumo, regras de pagamento e publicação de eventos.
* Health checks expostos e cobertura de testes automatizados.
* Documentação clara de variáveis sensíveis versus não sensíveis.
* Exemplos de como debugar e monitorar falhas nos eventos.

---


