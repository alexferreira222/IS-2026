# 📋 REFINAMENTO DOS ENDPOINTS DE APOSTAS

## ✅ MELHORIAS IMPLEMENTADAS

### **1. ApostasController - Novos Endpoints**

#### POST `/api/apostas`
- **Descrição**: Registar uma nova aposta
- **Melhorias**:
  - ✅ Data Annotations com validações automáticas
  - ✅ Logging estruturado de operações
  - ✅ CommandTimeout = 30s
  - ✅ Resposta estruturada com ganhos potenciais
  - ✅ Tratamento de exceções aprimorado

**Exemplo de Request:**
```json
{
  "idUtilizador": 1,
  "codigoJogo": "FUT-2026-0101",
  "tipoAposta": "1",
  "montante": 50.00,
  "odd": 2.50
}
```

**Exemplo de Response:**
```json
{
  "mensagem": "Aposta registada com sucesso. Saldo debitado.",
  "ganhosPotenciais": 125.00
}
```

---

#### GET `/api/apostas/utilizador/{idUtilizador}?pagina=1&tamanho=20`
- **Descrição**: Listar todas as apostas de um utilizador com paginação
- **Melhorias**:
  - ✅ Paginação (padrão: 20 registos/página)
  - ✅ Logging de consultas
  - ✅ Estrutura de resposta unificada
  - ✅ Cálculo automático de ganhos potenciais

**Response:**
```json
{
  "apostas": [
    {
      "idAposta": 1,
      "idUtilizador": 1,
      "codigoJogo": "FUT-2026-0101",
      "tipoAposta": "1",
      "montante": 50.00,
      "odd": 2.50,
      "ganhosPotenciais": 125.00,
      "estado": "Pendente",
      "dataRegisto": "2026-01-15T10:30:00Z"
    }
  ],
  "total": 15,
  "pagina": 1,
  "tamanho": 20
}
```

---

#### GET `/api/apostas/{idAposta}`
- **Descrição**: Obter detalhes completos de uma aposta específica
- **Melhorias**:
  - ✅ Resposta detalhada com todos os dados
  - ✅ Validação do ID
  - ✅ Tratamento de não encontrado (404)

---

#### GET `/api/apostas/jogo/{codigoJogo}?pagina=1&tamanho=20`
- **Descrição**: Listar todas as apostas de um jogo específico
- **Melhorias**:
  - ✅ Paginação
  - ✅ Análise de apostas por jogo
  - ✅ Suporte para estatísticas de jogo

---

### **2. PagamentosController - Novos Endpoints**

#### POST `/api/pagamentos/deposito`
- **Descrição**: Realizar depósito na conta
- **Melhorias**:
  - ✅ Data Annotations com limites (€0.01 - €10,000)
  - ✅ Logging estruturado
  - ✅ Resposta com timestamp

**Request:**
```json
{
  "idUtilizador": 1,
  "montante": 100.00
}
```

**Response:**
```json
{
  "mensagem": "Depósito de 100.00€ realizado com sucesso.",
  "montante": 100.00,
  "dataOperacao": "2026-01-15T10:30:00Z"
}
```

---

#### POST `/api/pagamentos/levantamento`
- **Descrição**: Realizar levantamento da conta
- **Melhorias**:
  - ✅ Validações iguais ao depósito
  - ✅ Tratamento de saldo insuficiente (erro 50000)
  - ✅ Logging de operações

---

#### GET `/api/pagamentos/saldo/{idUtilizador}`
- **Descrição**: Consultar saldo da conta
- **Melhorias**:
  - ✅ Resposta simples e rápida
  - ✅ Timestamp de consulta

**Response:**
```json
{
  "idUtilizador": 1,
  "saldo": 150.00,
  "dataConsulta": "2026-01-15T10:30:00Z"
}
```

---

### **3. JogosController - Aprimoramentos**

#### POST `/api/jogos`
- **Melhorias**:
  - ✅ Validação de data (não pode ser no passado)
  - ✅ Resposta 201 Created com Location header
  - ✅ Logging detalhado
  - ✅ Tratamento de null values

---

#### PUT `/api/jogos/{codigo}`
- **Melhorias**:
  - ✅ Validação de estados (0-3)
  - ✅ Validação de golos (não negativos)
  - ✅ Resposta com marcador
  - ✅ Logging de mudanças de estado

---

#### DELETE `/api/jogos/{codigo}`
- **Melhorias**:
  - ✅ Resposta estruturada
  - ✅ Logging de remoções

---

#### GET `/api/jogos/{codigo}` ⭐ NOVO
- **Descrição**: Obter detalhes completos de um jogo
- **Retorna**: Código, equipas, competição, estado, marcador

---

### **4. Data Annotations e Validações**

**RegistarApostaDto:**
```csharp
[Range(1, int.MaxValue)]                                    // ID válido
[RegularExpression(@"^FUT-\d{4}-\d{4}$")]                  // Código jogo
[RegularExpression(@"^[1X2]$")]                            // Tipo aposta
[Range(typeof(decimal), "0.01", "999999.99")]             // Montante
[Range(typeof(decimal), "1.01", "999999.99")]             // Odd
```

**DepositoDto / LevantamentoDto:**
```csharp
[Range(1, int.MaxValue)]                                   // ID válido
[Range(typeof(decimal), "0.01", "10000")]                 // Montante limitado
```

---

### **5. Logging Integrado**

Todos os endpoints incluem:
- ✅ Log de início de operação
- ✅ Log de sucesso
- ✅ Log de erros com stack trace

**Exemplo:**
```
[INF] Depositando 100.00€ para utilizador 1
[INF] Depósito realizado com sucesso para utilizador 1
[ERR] Erro ao processar depósito: Saldo insuficiente
```

---

### **6. CommandTimeout**

Todos os comandos SQL agora têm:
```csharp
cmd.CommandTimeout = 30; // Evita travamentos
```

---

## 📊 RESUMO DE ENDPOINTS

| Método | Rota | Descrição | Status |
|--------|------|-----------|--------|
| **APOSTAS** |
| POST | `/api/apostas` | Registar aposta | ✅ |
| GET | `/api/apostas/utilizador/{id}` | Listar apostas por utilizador | ✅ NOVO |
| GET | `/api/apostas/{id}` | Detalhes de aposta | ✅ NOVO |
| GET | `/api/apostas/jogo/{codigo}` | Listar apostas por jogo | ✅ NOVO |
| **PAGAMENTOS** |
| POST | `/api/pagamentos/deposito` | Fazer depósito | ✅ |
| POST | `/api/pagamentos/levantamento` | Fazer levantamento | ✅ NOVO |
| GET | `/api/pagamentos/saldo/{id}` | Consultar saldo | ✅ NOVO |
| **JOGOS** |
| POST | `/api/jogos` | Inserir jogo | ✅ |
| GET | `/api/jogos/{codigo}` | Detalhes do jogo | ✅ NOVO |
| PUT | `/api/jogos/{codigo}` | Atualizar jogo | ✅ |
| DELETE | `/api/jogos/{codigo}` | Remover jogo | ✅ |

---

## 🔒 SEGURANÇA

- ✅ Validações automáticas com Data Annotations
- ✅ Limites de montante (€0.01 - €10,000)
- ✅ Regex para formatos de código
- ✅ Tratamento estruturado de erros
- ✅ Logging de operações sensíveis
- ✅ CommandTimeout para evitar SQL Injection

---

## 📝 NOTAS

1. **Stored Procedures necessárias:**
   - `sp_Apostas_ListarPorUtilizador`
   - `sp_Apostas_ObterDetalhe`
   - `sp_Apostas_ListarPorJogo`
   - `sp_Pagamentos_Levantamento`
   - `sp_Pagamentos_ObterSaldo`
   - `sp_Apostas_ObterJogo`

2. **Paginação padrão:** 20 registos por página (máximo 100)

3. **Timestamps:** Todos em UTC (DateTime.UtcNow)

---

✨ **Projeto compilado com sucesso!**
