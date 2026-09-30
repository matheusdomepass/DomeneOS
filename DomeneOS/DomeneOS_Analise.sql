-- =======================================================
-- CASE DOMENEOS - ANÁLISE GERENCIAL
-- Objetivo: Extrair e consolidar indicadores operacionais
-- =======================================================

-- 1. Quantidade de Ordens de Serviço por Status
SELECT Status, COUNT(*) AS QuantidadeOS
FROM OrdensServico
GROUP BY Status
ORDER BY Status;

-- 2. Faturamento das Ordens de Serviço finalizadas
SELECT 
	SUM(Valor) AS FaturamentoTotal
	FROM OrdensServico
	WHERE Status = 3;

-- 3. Média das Ordens de Serviço finalizadas
SELECT
	CAST(AVG(Valor) AS DECIMAL(10,2)) AS Media
FROM OrdensServico
WHERE Status = 3;

-- 4. Quantidade de OS e valor por cliente
SELECT
	c.Nome AS Cliente,
	COUNT(o.Id) AS QuantidadeOS,
	SUM(o.Valor) AS ValorTotal
FROM Clientes c
INNER JOIN OrdensServico o
	ON c.Id = o.ClienteId
GROUP BY c.Nome
ORDER BY ValorTotal DESC;

-- 5. Produtos mais utilizados nas Ordens de Serviço
SELECT
	p.Nome AS Produto,
	SUM(osp.Quantidade) AS QuantidadeUtilizada
FROM OrdemServicoProdutos osp
INNER JOIN Produtos p
	ON osp.ProdutoId = p.Id
GROUP BY p.Nome
ORDER BY QuantidadeUtilizada DESC;