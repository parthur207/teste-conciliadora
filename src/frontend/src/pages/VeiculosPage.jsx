import React, { useEffect, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiGet, apiPost, apiPut, apiDelete } from '../api'
import { useToast } from '../Toast'
import { maskPlaca, maskAno} from '../utils/masksModuloVeiculos'

const FORM_VAZIO = { placa: '', modelo: '', ano: '', clienteId: '' }

export default function VeiculosPage() {
  const qc = useQueryClient()
  const toast = useToast()
  const [clienteIdFiltro, setClienteIdFiltro] = useState('')
  const [form, setForm] = useState(FORM_VAZIO)
  const [editando, setEditando] = useState(null) // { id, placa, modelo, ano, clienteId }
  const [erroCreate, setErroCreate] = useState('')
  const [erroEdit, setErroEdit] = useState('')

  const clientes = useQuery({
    queryKey: ['clientes-mini'],
    queryFn: () => apiGet('/api/clientes?pagina=1&tamanho=200')
  })

  const veiculos = useQuery({
    queryKey: ['veiculos', clienteIdFiltro],
    queryFn: () => apiGet(`/api/veiculos${clienteIdFiltro ? `?clienteId=${clienteIdFiltro}` : ''}`),
    refetchInterval: 30000
  })

  const create = useMutation({
    mutationFn: (data) => apiPost('/api/veiculos', data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['veiculos'] })
      setForm({ ...FORM_VAZIO, clienteId: form.clienteId })
      setErroCreate('')
      toast('Veículo cadastrado com sucesso.')
    },
    onError: (err) => setErroCreate(err.message)
  })

  const update = useMutation({
    mutationFn: ({ id, data }) => apiPut(`/api/veiculos/${id}`, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['veiculos'] })
      setEditando(null)
      setErroEdit('')
      toast('Veículo atualizado.')
    },
    onError: (err) => setErroEdit(err.message)
  })

  const remover = useMutation({
    mutationFn: (id) => apiDelete(`/api/veiculos/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['veiculos'] })
      toast('Veículo excluído.')
    },
    onError: (err) => toast(err.message, 'error')
  })

  // Define o primeiro cliente como padrão ao carregar
  useEffect(() => {
    if (clientes.data?.itens?.length && !form.clienteId) {
      const primeiroId = clientes.data.itens[0].id
      setForm(f => ({ ...f, clienteId: primeiroId }))
    }
  }, [clientes.data])

  function iniciarEdicao(v) {
    setEditando({
      id: v.id,
      placa: v.placa,
      modelo: v.modelo || '',
      ano: v.ano != null ? String(v.ano) : '',
      clienteId: v.clienteId
    })
    setErroEdit('')
  }

  function salvarEdicao() {
    if (!editando.clienteId) { setErroEdit('Selecione um cliente.'); return }
    update.mutate({
      id: editando.id,
      data: {
        placa: editando.placa,
        modelo: editando.modelo,
        ano: editando.ano ? Number(editando.ano) : null,
        clienteId: editando.clienteId
      }
    })
  }

  function salvarNovo() {
    if (!form.clienteId) { setErroCreate('Selecione um cliente.'); return }
    create.mutate({
      placa: form.placa,
      modelo: form.modelo,
      ano: form.ano ? Number(form.ano) : null,
      clienteId: form.clienteId
    })
  }

  const listaClientes = clientes.data?.itens || []

  return (
    <div>
      <h2>Veículos</h2>

      <div className="section">
        <div style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
          <label>Filtrar por cliente:</label>
          <select value={clienteIdFiltro} onChange={e => setClienteIdFiltro(e.target.value)}>
            <option value="">Todos os clientes</option>
            {listaClientes.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
          </select>
        </div>
      </div>

      <h3>Novo veículo</h3>
      <div className="section">
        {erroCreate && <p style={{ color: '#f87171', marginBottom: 8 }}>{erroCreate}</p>}
        <div className="grid grid-4">
          <input placeholder="Placa *" value={form.placa} onChange={e => setForm({ ...form, placa: maskPlaca(e.target.value) })} />
          <input placeholder="Modelo" value={form.modelo} onChange={e => setForm({ ...form, modelo: e.target.value })} />
          <input placeholder="Ano" value={form.ano} onChange={e => setForm({ ...form, ano: maskAno(e.target.value) })} />
          <select value={form.clienteId} onChange={e => setForm({ ...form, clienteId: e.target.value })}>
            <option value="">Selecione o cliente *</option>
            {listaClientes.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
          </select>
          <div /><div /><div />
          <button onClick={salvarNovo} disabled={create.isPending}>
            {create.isPending ? 'Salvando...' : 'Salvar'}
          </button>
        </div>
      </div>

      {editando && (
        <>
          <h3 style={{ marginTop: 16 }}>Editar veículo</h3>
          <div className="section" style={{ borderLeft: '3px solid var(--accent)' }}>
            {erroEdit && <p style={{ color: '#f87171', marginBottom: 8 }}>{erroEdit}</p>}
            <div className="grid grid-4">
              <input placeholder="Placa *" value={editando.placa} onChange={e => setEditando({ ...editando, placa: maskPlaca(e.target.value) })} />
              <input placeholder="Modelo" value={editando.modelo} onChange={e => setEditando({ ...editando, modelo: e.target.value })} />
              <input placeholder="Ano" value={editando.ano} onChange={e => setEditando({ ...editando, ano: maskAno(e.target.value) })} />
              <select value={editando.clienteId} onChange={e => setEditando({ ...editando, clienteId: e.target.value })}>
                <option value="">Selecione o cliente *</option>
                {listaClientes.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
              </select>
              <div /><div /><div />
              <div style={{ display: 'flex', gap: 8 }}>
                <button onClick={salvarEdicao} disabled={update.isPending}>
                  {update.isPending ? 'Salvando...' : 'Confirmar'}
                </button>
                <button className="btn-ghost" onClick={() => { setEditando(null); setErroEdit('') }}>Cancelar</button>
              </div>
            </div>
          </div>
        </>
      )}

      <h3 style={{ marginTop: 16 }}>Lista</h3>
      <div className="section">
        {veiculos.isLoading ? <p>Carregando...</p> : veiculos.data?.length === 0 ? (
          <p style={{ color: 'var(--muted)' }}>Nenhum veículo encontrado.</p>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Placa</th>
                <th>Modelo</th>
                <th>Ano</th>
                <th>Cliente</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              {veiculos.data?.map(v => (
                <tr key={v.id} style={editando?.id === v.id ? { background: 'rgba(99,102,241,0.08)' } : {}}>
                  <td>{v.placa}</td>
                  <td>{v.modelo || '-'}</td>
                  <td>{v.ano ?? '-'}</td>
                  <td>{v.clienteNome || '-'}</td>
                  <td style={{ display: 'flex', gap: 8 }}>
                    <button className="btn-ghost" onClick={() => iniciarEdicao(v)}>Editar</button>
                    <button className="btn-ghost" onClick={() => {
                      if (window.confirm(`Excluir veículo "${v.placa}"?`)) remover.mutate(v.id)
                    }}>Excluir</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}
