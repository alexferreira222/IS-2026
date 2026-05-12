const API_URL = 'https://localhost:7083/api';
const UTILIZADOR_ID = 1;

// Estado global
let apostaAtual = { jogoId: "", equipa: "", tipo: "", odd: 0 };
let apostaParaCancelar = null;
let intervaloAoVivo = null;
let tabAtiva = 'jogos';

// ─── INICIALIZAÇÃO ────────────────────────────────────────────────────────────

window.onload = () => {
    carregarTudo();
    iniciarAtualizacaoAoVivo();
};

async function carregarTudo() {
    animarRefresh(true);
    esconderErro();
    await Promise.all([carregarSaldo(), carregarJogos()]);
    animarRefresh(false);
}

function animarRefresh(ativo) {
    const icon = document.getElementById('icon-refresh');
    if (ativo) icon.classList.add('spinner');
    else icon.classList.remove('spinner');
}

// ─── TABS ─────────────────────────────────────────────────────────────────────

function mudarTab(tab, btn) {
    tabAtiva = tab;
    document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    ['jogos', 'minhas-apostas', 'ao-vivo'].forEach(t => {
        const el = document.getElementById(`tab-${t}`);
        if (t === tab) { el.classList.remove('hidden'); el.classList.add('fade-in'); }
        else el.classList.add('hidden');
    });
    if (tab === 'minhas-apostas') carregarMinhasApostas();
    if (tab === 'ao-vivo') carregarAoVivo();
}

// ─── SALDO ────────────────────────────────────────────────────────────────────

async function carregarSaldo() {
    try {
        const res = await fetch(`${API_URL}/Pagamentos/saldo/${UTILIZADOR_ID}`);
        if (!res.ok) throw new Error();
        const data = await res.json();
        document.getElementById('valor-saldo').innerText = parseFloat(data.saldo).toFixed(2).replace('.', ',');
    } catch {
        document.getElementById('valor-saldo').innerText = '—';
    }
}

// ─── JOGOS ────────────────────────────────────────────────────────────────────

async function carregarJogos() {
    const container = document.getElementById('jogos-container');
    try {
        const res = await fetch(`${API_URL}/Jogos`);
        if (!res.ok) throw new Error('Erro ao carregar jogos');
        const jogos = await res.json();

        const visiveis = jogos.filter(j => j.estado === 1 || j.estado === 2);

        if (visiveis.length === 0) {
            container.innerHTML = `
                <div class="col-span-full text-center py-16">
                    <div class="text-4xl mb-3">🏟</div>
                    <p class="text-slate-400">Sem jogos disponíveis neste momento.</p>
                    <p class="text-slate-600 text-sm mt-1">Aguarda a próxima jornada.</p>
                </div>`;
            return;
        }

        container.innerHTML = visiveis.map(jogo => renderJogoCard(jogo)).join('');
    } catch (e) {
        mostrarErro('Não foi possível carregar os jogos. Verifica se a API está a correr.');
        container.innerHTML = '';
    }
}

function renderJogoCard(jogo) {
    const estadoLabel = jogo.estado === 1 ? 'Agendado' : 'Em curso';
    const estadoClass = jogo.estado === 1 ? 'estado-badge-1' : 'estado-badge-2';
    const emCurso = jogo.estado === 2;

    return `
        <div class="bg-card rounded-xl border border-border p-4 hover:border-slate-600 transition-all fade-in">
            <div class="flex justify-between items-start mb-3">
                <span class="text-xs font-mono text-slate-500">${jogo.codigo}</span>
                <span class="text-xs font-display font-bold px-2 py-1 rounded-full ${estadoClass}">
                    ${emCurso ? '🔴 ' : ''}${estadoLabel}
                </span>
            </div>
            <div class="flex justify-between items-center font-display font-black text-white mb-1">
                <span class="text-base">${jogo.equipaCasa}</span>
                ${emCurso
            ? `<span class="text-gold text-lg px-2">${jogo.golosCasa ?? 0} — ${jogo.golosFora ?? 0}</span>`
            : `<span class="text-slate-500 text-sm px-2">VS</span>`
        }
                <span class="text-base">${jogo.equipaFora}</span>
            </div>
            <div class="text-xs text-slate-500 text-center mb-4">${jogo.tipoCompeticao || ''}</div>
            <div class="grid grid-cols-3 gap-2">
                <button onclick="adicionarBoletim('${jogo.codigo}', '${jogo.equipaCasa}', '1', 2.10)"
                    class="odd-btn bg-darkbg border border-border rounded-lg py-2 text-center text-sm font-bold text-slate-300">
                    <div class="text-xs text-slate-500">1</div>2.10
                </button>
                <button onclick="adicionarBoletim('${jogo.codigo}', 'Empate', 'X', 3.20)"
                    class="odd-btn bg-darkbg border border-border rounded-lg py-2 text-center text-sm font-bold text-slate-300">
                    <div class="text-xs text-slate-500">X</div>3.20
                </button>
                <button onclick="adicionarBoletim('${jogo.codigo}', '${jogo.equipaFora}', '2', 2.80)"
                    class="odd-btn bg-darkbg border border-border rounded-lg py-2 text-center text-sm font-bold text-slate-300">
                    <div class="text-xs text-slate-500">2</div>2.80
                </button>
            </div>
        </div>`;
}

// ─── BOLETIM ──────────────────────────────────────────────────────────────────

function adicionarBoletim(jogoId, equipa, tipo, odd) {
    apostaAtual = { jogoId, equipa, tipo, odd };
    document.getElementById('boletim-vazio').classList.add('hidden');
    document.getElementById('boletim-ativo').classList.remove('hidden');
    document.getElementById('bol-jogo').innerText = jogoId;
    document.getElementById('bol-equipa').innerText = equipa;
    document.getElementById('bol-odd').innerText = odd.toFixed(2);
    calcularGanhos();
}

function limparBoletim() {
    apostaAtual = { jogoId: "", equipa: "", tipo: "", odd: 0 };
    document.getElementById('boletim-vazio').classList.remove('hidden');
    document.getElementById('boletim-ativo').classList.add('hidden');
}

function calcularGanhos() {
    const valor = parseFloat(document.getElementById('bol-valor').value) || 0;
    const ganhos = valor * apostaAtual.odd;
    document.getElementById('bol-ganhos').innerText = ganhos.toFixed(2).replace('.', ',') + ' €';
}

async function confirmarAposta() {
    const valor = parseFloat(document.getElementById('bol-valor').value);
    if (!valor || valor <= 0) { mostrarErro('Introduz um valor válido.'); return; }
    if (!apostaAtual.jogoId) { mostrarErro('Seleciona um jogo primeiro.'); return; }

    const payload = {
        UtilizadorId: UTILIZADOR_ID,
        CodigoJogo: apostaAtual.jogoId,
        TipoAposta: apostaAtual.tipo,
        Montante: valor,
        OddMomento: apostaAtual.odd
    };

    try {
        const res = await fetch(`${API_URL}/Apostas`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            limparBoletim();
            await carregarSaldo();
            mostrarSucesso('✅ Aposta registada com sucesso!');
        } else {
            const err = await res.json();
            mostrarErro(err.erro || 'Erro ao registar aposta.');
        }
    } catch {
        mostrarErro('Erro de ligação. Verifica se a API está a correr.');
    }
}

// ─── MINHAS APOSTAS ───────────────────────────────────────────────────────────

async function carregarMinhasApostas() {
    const container = document.getElementById('apostas-container');
    container.innerHTML = `
        <div class="flex justify-center py-10">
            <div class="w-7 h-7 border-2 border-gold border-t-transparent rounded-full spinner"></div>
        </div>`;

    try {
        const res = await fetch(`${API_URL}/Apostas/utilizador/${UTILIZADOR_ID}`);
        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.erro || 'Erro ao carregar apostas');
        }
        const data = await res.json();

        // A API devolve { apostas: [...], total, pagina, tamanho }
        const apostas = data.apostas ?? data;

        if (!apostas || !apostas.length) {
            container.innerHTML = `
                <div class="text-center py-16">
                    <div class="text-4xl mb-3">🎫</div>
                    <p class="text-slate-400">Ainda não fizeste nenhuma aposta.</p>
                </div>`;
            return;
        }

        container.innerHTML = apostas.map(a => renderApostaCard(a)).join('');
    } catch (e) {
        container.innerHTML = `<p class="text-red-400 text-sm text-center py-8">Erro ao carregar apostas: ${e.message}</p>`;
    }
}

function renderApostaCard(aposta) {
    // A API devolve Estado como string ("Pendente", "Ganha", etc.) via ApostaResponseDto
    const estadoStr = (aposta.estado || aposta.Estado || '').toLowerCase();
    const estadoMap = {
        'pendente': { label: 'Pendente', cls: 'bg-blue-900/40 text-blue-300 border-blue-800', icon: '⏳' },
        'ganha': { label: 'Ganha', cls: 'bg-green-900/40 text-green-300 border-green-800', icon: '✅' },
        'perdida': { label: 'Perdida', cls: 'bg-red-900/40 text-red-300 border-red-800', icon: '❌' },
        'cancelada': { label: 'Cancelada', cls: 'bg-slate-800 text-slate-400 border-slate-700', icon: '🚫' },
    };
    const e = estadoMap[estadoStr] || { label: estadoStr, cls: 'bg-slate-800 text-slate-400 border-slate-700', icon: '❓' };
    const podeCancelar = estadoStr === 'pendente';

    // Campos podem vir em camelCase ou PascalCase dependendo da serialização
    const idAposta = aposta.idAposta ?? aposta.IdAposta;
    const codigoJogo = aposta.codigoJogo ?? aposta.CodigoJogo ?? '—';
    const tipoAposta = aposta.tipoAposta ?? aposta.TipoAposta ?? '';
    const montante = parseFloat(aposta.montante ?? aposta.Montante ?? 0);
    const odd = parseFloat(aposta.oddMomento ?? aposta.OddMomento ?? 0);

    const tipoLabel = tipoAposta === '1' ? 'Vitória Casa'
        : tipoAposta === '2' ? 'Vitória Fora'
            : tipoAposta === 'X' ? 'Empate'
                : tipoAposta;

    return `
        <div class="bg-card rounded-xl border border-border p-4 fade-in">
            <div class="flex justify-between items-start mb-2">
                <div>
                    <div class="text-xs font-mono text-slate-500 mb-1">${codigoJogo}</div>
                    <div class="font-display font-black text-white text-base">${tipoLabel}</div>
                </div>
                <div class="flex items-center gap-2">
                    <span class="text-xs font-display font-bold px-2 py-1 rounded-full border ${e.cls}">${e.icon} ${e.label}</span>
                    ${podeCancelar ? `
                    <button onclick="abrirModalCancelar(${idAposta}, '${codigoJogo}', '${tipoLabel}', ${montante})"
                        class="text-xs bg-red-900/40 hover:bg-red-800 text-red-300 border border-red-800 px-2 py-1 rounded-lg font-bold transition-all">
                        Cancelar
                    </button>` : ''}
                </div>
            </div>
            <div class="grid grid-cols-3 gap-3 mt-3 pt-3 border-t border-border text-center text-sm">
                <div>
                    <div class="text-slate-500 text-xs">Montante</div>
                    <div class="font-bold text-white">${montante.toFixed(2)} €</div>
                </div>
                <div>
                    <div class="text-slate-500 text-xs">Odd</div>
                    <div class="font-bold text-gold">${odd.toFixed(2)}</div>
                </div>
                <div>
                    <div class="text-slate-500 text-xs">Retorno pot.</div>
                    <div class="font-bold text-green-400">${(montante * odd).toFixed(2)} €</div>
                </div>
            </div>
        </div>`;
}

// ─── CANCELAR APOSTA ──────────────────────────────────────────────────────────

function abrirModalCancelar(id, codigoJogo, tipo, montante) {
    apostaParaCancelar = id;
    document.getElementById('cancelar-info').innerHTML = `
        <div><span class="text-slate-500">Jogo:</span> <span class="text-white font-mono">${codigoJogo}</span></div>
        <div><span class="text-slate-500">Aposta:</span> <span class="text-white">${tipo}</span></div>
        <div><span class="text-slate-500">Montante:</span> <span class="text-white">${parseFloat(montante).toFixed(2)} €</span></div>`;
    abrirModal('modal-cancelar');
}

async function confirmarCancelamento() {
    if (!apostaParaCancelar) return;
    try {
        // Agora fazemos um POST para a rota certa do teu backend!
        const res = await fetch(`${API_URL}/Apostas/${apostaParaCancelar}/cancelar`, {
            method: 'POST'
        });

        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.erro || 'Erro ao cancelar aposta');
        }

        fecharModal('modal-cancelar');
        mostrarSucesso('Aposta cancelada com sucesso!');
        apostaParaCancelar = null;

        // Atualiza a interface
        carregarMinhasApostas();
        carregarSaldo();
    } catch (e) {
        fecharModal('modal-cancelar');
        mostrarErro(e.message);
    }
}

// ─── AO VIVO ──────────────────────────────────────────────────────────────────

async function carregarAoVivo() {
    const container = document.getElementById('aovivo-container');
    try {
        const res = await fetch(`${API_URL}/Jogos`);
        if (!res.ok) throw new Error();
        const jogos = await res.json();
        const aoVivo = jogos.filter(j => j.estado === 2);

        if (!aoVivo.length) {
            container.innerHTML = `
                <div class="text-center py-16">
                    <div class="text-4xl mb-3">📺</div>
                    <p class="text-slate-400">Nenhum jogo em curso neste momento.</p>
                </div>`;
            return;
        }

        container.innerHTML = aoVivo.map(jogo => `
            <div class="bg-card rounded-xl border border-green-900/50 p-4 fade-in">
                <div class="flex justify-between items-center mb-1">
                    <span class="text-xs font-mono text-slate-500">${jogo.codigo}</span>
                    <span class="flex items-center gap-1 text-xs text-green-400 font-display font-bold">
                        <span class="w-2 h-2 bg-green-400 rounded-full pulse-green inline-block"></span> AO VIVO
                    </span>
                </div>
                <div class="flex justify-between items-center font-display font-black text-white text-lg">
                    <span>${jogo.equipaCasa}</span>
                    <span class="text-gold text-2xl px-4">${jogo.golosCasa ?? 0} — ${jogo.golosFora ?? 0}</span>
                    <span>${jogo.equipaFora}</span>
                </div>
                <div class="text-xs text-slate-500 text-center mt-1">${jogo.tipoCompeticao || ''}</div>
            </div>`).join('');
    } catch {
        container.innerHTML = `<p class="text-red-400 text-sm text-center py-8">Erro ao carregar jogos ao vivo.</p>`;
    }
}

function iniciarAtualizacaoAoVivo() {
    intervaloAoVivo = setInterval(() => {
        if (tabAtiva === 'ao-vivo') carregarAoVivo();
        // Atualiza também os jogos em background para manter marcador atualizado
        carregarJogos();
        carregarSaldo();
    }, 15000);
}

// ─── DEPÓSITO ─────────────────────────────────────────────────────────────────

function abrirModalDeposito() {
    document.getElementById('deposito-msg').classList.add('hidden');
    abrirModal('modal-deposito');
}

function definirDeposito(valor) {
    document.getElementById('deposito-valor').value = valor;
}

async function confirmarDeposito() {
    const valor = parseFloat(document.getElementById('deposito-valor').value);
    const msgEl = document.getElementById('deposito-msg');

    if (!valor || valor <= 0) {
        msgEl.className = 'text-center text-sm py-2 rounded-lg bg-red-900/40 text-red-300';
        msgEl.innerText = 'Introduz um valor válido.';
        msgEl.classList.remove('hidden');
        return;
    }

    try {
        const res = await fetch(`${API_URL}/Pagamentos/deposito`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            // Enviamos tanto camelCase como PascalCase para garantir compatibilidade
            body: JSON.stringify({ utilizadorId: UTILIZADOR_ID, montante: valor, UtilizadorId: UTILIZADOR_ID, Montante: valor })
        });

        if (res.ok) {
            msgEl.className = 'text-center text-sm py-2 rounded-lg bg-green-900/40 text-green-300';
            msgEl.innerText = `✅ ${valor.toFixed(2)} € depositados com sucesso!`;
            msgEl.classList.remove('hidden');
            await carregarSaldo();
            setTimeout(() => fecharModal('modal-deposito'), 1500);
        } else {
            const err = await res.json();
            msgEl.className = 'text-center text-sm py-2 rounded-lg bg-red-900/40 text-red-300';
            msgEl.innerText = err.erro || JSON.stringify(err);
            msgEl.classList.remove('hidden');
        }
    } catch {
        msgEl.className = 'text-center text-sm py-2 rounded-lg bg-red-900/40 text-red-300';
        msgEl.innerText = 'Erro de ligação.';
        msgEl.classList.remove('hidden');
    }
}

// ─── MODAIS ───────────────────────────────────────────────────────────────────

function abrirModal(id) { document.getElementById(id).classList.remove('hidden'); }
function fecharModal(id) { document.getElementById(id).classList.add('hidden'); }
function fecharModalSeFora(event, id) {
    if (event.target === document.getElementById(id)) fecharModal(id);
}

// ─── MENSAGENS ────────────────────────────────────────────────────────────────

function mostrarErro(msg) {
    const el = document.getElementById('mensagens-erro');
    el.innerText = msg;
    el.classList.remove('hidden');
    el.className = 'bg-red-900/40 text-red-300 p-3 rounded-lg border border-red-800 text-sm';
    setTimeout(() => el.classList.add('hidden'), 5000);
}

function mostrarSucesso(msg) {
    const el = document.getElementById('mensagens-erro');
    el.innerText = msg;
    el.classList.remove('hidden');
    el.className = 'bg-green-900/40 text-green-300 p-3 rounded-lg border border-green-800 text-sm';
    setTimeout(() => el.classList.add('hidden'), 4000);
}

function esconderErro() { document.getElementById('mensagens-erro').classList.add('hidden'); }