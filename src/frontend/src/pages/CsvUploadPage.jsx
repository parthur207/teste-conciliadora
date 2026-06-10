import React, { useState } from 'react'
import { useToast } from '../Toast'

export default function CsvUploadPage() {
  const toast = useToast()
  const [resultado, setResultado] = useState(null)
  const [carregando, setCarregando] = useState(false)
  const [erro, setErro] = useState('')

  async function handleUpload(e) {
    e.preventDefault()
    const file = e.target.file.files[0]

  if (!file) {
    setErro('Selecione um arquivo CSV.')
    return
  }

  const extensao = file.name.split('.').pop()?.toLowerCase()

  if (extensao !== 'csv') {
    setErro('Arquivo inválido. Selecione um arquivo com extensão .csv.')
    toast('Arquivo inválido. Apenas arquivos CSV são permitidos.', 'error')
    return
  }

    setCarregando(true)
    setErro('')
    setResultado(null)

    const fd = new FormData()
    fd.append('file', file)

    try {
      const r = await fetch((import.meta.env.VITE_API_URL || 'http://localhost:5000') + '/api/import/csv', {
        method: 'POST',
        body: fd
      })
      const j = await r.json()
      if (!r.ok) {
        setErro(typeof j === 'string' ? j : JSON.stringify(j))
      } else {
        setResultado(j)
        if (j.totalErros === 0) {
          toast(`Importação concluída: ${j.inseridos} linha${j.inseridos !== 1 ? 's' : ''} inserida${j.inseridos !== 1 ? 's' : ''}.`)
        } else {
          toast(`Importação concluída com ${j.totalErros} erro${j.totalErros !== 1 ? 's' : ''}. Veja os detalhes abaixo.`, 'error')
        }
      }
    } catch (ex) {
      setErro('Falha ao conectar com o servidor.')
    } finally {
      setCarregando(false)
    }
  }

  return (
    <div>
      <h2>Importar CSV</h2>

      <div className="section">
        <p style={{ color: 'var(--muted)', marginBottom: 12, fontSize: '0.9em' }}>
          Formato esperado: <code>placa,modelo,ano,cliente_nome,cliente_telefone,cliente_endereco,mensalista,valor_mensalidade</code>
        </p>
        <form onSubmit={handleUpload} style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
          <input type="file" name="file" accept=".csv" />
          <button type="submit" disabled={carregando}>
            {carregando ? 'Enviando...' : 'Enviar'}
          </button>
        </form>
        {erro && <p style={{ color: '#f87171', marginTop: 8 }}>{erro}</p>}
      </div>

      {resultado && (
        <>
          <h3 style={{ marginTop: 16 }}>Resultado</h3>
          <div className="section">
            <div style={{ display: 'flex', gap: 24, marginBottom: 16 }}>
              <Stat label="Processados" value={resultado.processados} color="var(--muted)" />
              <Stat label="Inseridos" value={resultado.inseridos} color="#4ade80" />
              <Stat label="Erros" value={resultado.totalErros} color={resultado.totalErros > 0 ? '#f87171' : '#4ade80'} />
            </div>

            {resultado.erros?.length > 0 && (
              <>
                <h4 style={{ marginBottom: 8 }}>Erros encontrados</h4>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                  {resultado.erros.map((e, i) => (
                    <div key={i} style={{
                      background: 'rgba(248,113,113,0.08)',
                      border: '1px solid rgba(248,113,113,0.3)',
                      borderRadius: 8,
                      padding: '8px 12px',
                      display: 'flex',
                      gap: 12,
                      alignItems: 'flex-start'
                    }}>
                      <span style={{
                        background: '#f87171',
                        color: '#0b0c0e',
                        borderRadius: 4,
                        padding: '2px 6px',
                        fontWeight: 700,
                        fontSize: '0.8em',
                        whiteSpace: 'nowrap'
                      }}>
                        Linha {e.linha}
                      </span>
                      <span style={{ color: '#fca5a5' }}>{e.motivo}</span>
                    </div>
                  ))}
                </div>
              </>
            )}

            {resultado.totalErros === 0 && (
              <p style={{ color: '#4ade80' }}>Importação concluída sem erros.</p>
            )}
          </div>
        </>
      )}
    </div>
  )
}

function Stat({ label, value, color }) {
  return (
    <div style={{ textAlign: 'center' }}>
      <div style={{ fontSize: '2em', fontWeight: 700, color }}>{value}</div>
      <div style={{ color: 'var(--muted)', fontSize: '0.85em' }}>{label}</div>
    </div>
  )
}
