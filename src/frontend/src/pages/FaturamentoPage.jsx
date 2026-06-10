import React, { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { apiGet, apiPost } from '../api'
import { useToast } from '../Toast'
import { maskCompetencia } from '../utils/masksModuloFaturamento'

export default function FaturamentoPage() {
  const toast = useToast()
  const [comp, setComp] = useState('')
  const [gerando, setGerando] = useState(false)
  const [erroGerar, setErroGerar] = useState('')

  const compValida = comp.length === 7

  const faturas = useQuery({
    queryKey: ['faturas', comp],
    queryFn: () => apiGet(`/api/faturas?competencia=${comp}`),
    refetchInterval: 30000,
    enabled: compValida
  })

  async function gerarFaturas() {
    setGerando(true)
    setErroGerar('')
    try {
      const res = await apiPost('/api/faturas/gerar', { competencia: comp })
      await faturas.refetch()
      if (res.criadas === 0) {
        setErroGerar('Nenhuma fatura nova gerada. Verifique se já existem faturas para essa competência ou se há clientes mensalistas com veículos.')
      } else {
        toast(`${res.criadas} fatura${res.criadas > 1 ? 's' : ''} gerada${res.criadas > 1 ? 's' : ''} com sucesso.`)
      }
    } catch (err) {
      setErroGerar(err.message)
    } finally {
      setGerando(false)
    }
  }

  return (
    <div>
      <h2>Faturamento</h2>

      <div className="section">
        <div style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
          <input
            value={comp}
            onChange={e => setComp(maskCompetencia(e.target.value))}
            placeholder="yyyy-MM"
            style={{ width: 120 }}
          />
          <button onClick={gerarFaturas} disabled={gerando || !compValida}>
            {gerando ? 'Gerando...' : 'Gerar faturas'}
          </button>
        </div>
        {erroGerar && <p style={{ color: '#f87171', marginTop: 8 }}>{erroGerar}</p>}
      </div>

      <h3 style={{ marginTop: 16 }}>Faturas{compValida ? ` — ${comp}` : ''}</h3>
      <div className="section">
        {!compValida ? (
          <p style={{ color: 'var(--muted)' }}>Informe a competência no formato yyyy-MM para visualizar as faturas.</p>
        ) : faturas.isLoading ? <p>Carregando...</p> : (
          <>
            {(!faturas.data || faturas.data.length === 0) && (
              <p style={{ color: 'var(--muted)' }}>Nenhuma fatura encontrada para essa competência.</p>
            )}
            {faturas.data?.length > 0 && (
              <table>
                <thead>
                  <tr>
                    <th>Cliente</th>
                    <th>Competência</th>
                    <th>Valor</th>
                    <th>Qtd Veículos</th>
                    <th>Observação</th>
                    <th>Placas</th>
                  </tr>
                </thead>
                <tbody>
                  {faturas.data.map(f => (<FaturaRow key={f.id} f={f} />))}
                </tbody>
              </table>
            )}
          </>
        )}
      </div>
    </div>
  )
}

function FaturaRow({ f }) {
  const [show, setShow] = useState(false)
  const [placas, setPlacas] = useState([])

  return (
    <tr>
      <td>{f.clienteNome || f.clienteId}</td>
      <td>{f.competencia}</td>
      <td>R$ {Number(f.valor).toFixed(2)}</td>
      <td>{f.qtdVeiculos}</td>
      <td style={{ fontSize: '0.85em', color: 'var(--muted)' }}>{f.observacao || '-'}</td>
      <td>
        <button className="btn-ghost" onClick={async () => {
          if (!show) {
            const r = await apiGet(`/api/faturas/${f.id}/placas`)
            setPlacas(r)
          }
          setShow(s => !s)
        }}>{show ? 'ocultar' : 'detalhar'}</button>
        {show && (
          <div style={{ marginTop: 6, fontSize: '0.9em' }}>
            {placas.length > 0 ? placas.join(', ') : 'Nenhuma placa associada.'}
          </div>
        )}
      </td>
    </tr>
  )
}
