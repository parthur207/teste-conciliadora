import React, { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiGet, apiPost, apiPut, apiDelete } from '../api'
import { useToast } from '../Toast'
import { maskTelefone, maskValorMensalidade } from '../utils/masksModuloClientes';

const FORM_VAZIO = { nome: '', telefone: '', endereco: '', mensalista: false, valorMensalidade: '' }

export default function ClientesPage() {
  const qc = useQueryClient()
  const toast = useToast()
  const [filtro, setFiltro] = useState('')
  const [mensalistaFiltro, setMensalistaFiltro] = useState('all')
  const [form, setForm] = useState(FORM_VAZIO)
  const [editando, setEditando] = useState(null) // { id, ...campos }
  const [erroCreate, setErroCreate] = useState('')
  const [erroEdit, setErroEdit] = useState('')

  const q = useQuery({
    queryKey: ['clientes', filtro, mensalistaFiltro],
    queryFn: () => apiGet(`/api/clientes?pagina=1&tamanho=20&filtro=${encodeURIComponent(filtro)}&mensalista=${mensalistaFiltro}`),
    refetchInterval: 30000
  })

  const create = useMutation({
    mutationFn: (data) => apiPost('/api/clientes', data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['clientes'] })
      setForm(FORM_VAZIO)
      setErroCreate('')
      toast('Cliente cadastrado com sucesso.')
    },
    onError: (err) => setErroCreate(err.message)
  })

  const update = useMutation({
    mutationFn: ({ id, data }) => apiPut(`/api/clientes/${id}`, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['clientes'] })
      setEditando(null)
      setErroEdit('')
      toast('Cliente atualizado.')
    },
    onError: (err)=> setErroEdit(err.message)
  })

  const remover = useMutation({
    mutationFn: (id) => apiDelete(`/api/clientes/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['clientes'] })
      toast('Cliente excluído.')
    },
    onError: (err) => toast(err.message, 'error')
  })

  function iniciarEdicao(c) {
    setEditando({
      id: c.id,
      nome: c.nome,
      telefone: c.telefone || '',
      endereco: c.endereco || '',
      mensalista: c.mensalista,
      valorMensalidade: c.valorMensalidade != null ? String(c.valorMensalidade) : ''
    })
    setErroEdit('')
  }

  function salvarEdicao() {
    if (!editando.nome.trim()) { setErroEdit('Nome é obrigatório.'); return }
    update.mutate({
      id: editando.id,
      data: {
        nome: editando.nome,
        telefone: editando.telefone,
        endereco: editando.endereco,
        mensalista: editando.mensalista,
        valorMensalidade: editando.valorMensalidade ? Number(editando.valorMensalidade) : null
      }
    })
  }

  function salvarNovo() {
    if (!form.nome.trim()) { setErroCreate('Nome é obrigatório.'); return }
    create.mutate({
      nome: form.nome,
      telefone: form.telefone,
      endereco: form.endereco,
      mensalista: form.mensalista,
      valorMensalidade: form.valorMensalidade ? Number(form.valorMensalidade) : null
    })
  }

  return (
    <div>
      <h2>Clientes</h2>

      <div className="section">
        <div className="grid grid-3">
          <input placeholder="Buscar por nome" value={filtro} onChange={e => setFiltro(e.target.value)} />
          <select value={mensalistaFiltro} onChange={e => setMensalistaFiltro(e.target.value)}>
            <option value="all">Todos</option>
            <option value="true">Mensalistas</option>
            <option value="false">Não mensalistas</option>
          </select>
          <div />
        </div>
      </div>

      <h3>Novo cliente</h3>
      <div className="section">
        {erroCreate && <p style={{ color: '#f87171', marginBottom: 8 }}>{erroCreate}</p>}
        <div className="grid grid-4">
          <input placeholder="Nome *" value={form.nome} onChange={e => setForm({ ...form, nome: e.target.value })} />
          <input placeholder="Telefone" value={form.telefone} onChange={e => setForm({...form, telefone: maskTelefone(e.target.value)})}/>          <input placeholder="Endereço" value={form.endereco} onChange={e => setForm({ ...form, endereco: e.target.value })} />
          <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <input type="checkbox" checked={form.mensalista} onChange={e => setForm({ ...form, mensalista: e.target.checked })} />
            Mensalista
          </label>
          <input placeholder="Valor mensalidade" value={form.valorMensalidade} disabled={!form.mensalista} onChange={e => setForm({...form, valorMensalidade: maskValorMensalidade(e.target.value)})}/>       <div /><div />
          <button onClick={salvarNovo} disabled={create.isPending}>
            {create.isPending ? 'Salvando...' : 'Salvar'}
          </button>
        </div>
      </div>

      {editando && (
        <>
          <h3 style={{ marginTop: 16 }}>Editar cliente</h3>
          <div className="section" style={{ borderLeft: '3px solid var(--accent)' }}>
            {erroEdit && <p style={{ color: '#f87171', marginBottom: 8 }}>{erroEdit}</p>}
            <div className="grid grid-4">
              <input placeholder="Nome *" value={editando.nome} onChange={e => setEditando({ ...editando, nome: e.target.value })} />
              <input placeholder="Telefone" value={editando.telefone} onChange={e => setEditando({ ...editando, telefone: maskTelefone(e.target.value) })} />
              <input placeholder="Endereço" value={editando.endereco} onChange={e => setEditando({ ...editando, endereco: e.target.value })} />
              <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <input type="checkbox" checked={editando.mensalista} onChange={e => setEditando({ ...editando, mensalista: e.target.checked })} />
                Mensalista
              </label>
              <input placeholder="Valor mensalidade" value={editando.valorMensalidade} disabled={!editando.mensalista} onChange={e => setEditando({...editando, valorMensalidade: maskValorMensalidade(e.target.value)})}/>              <div /><div />
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
        {q.isLoading ? <p>Carregando...</p> : q.data?.itens?.length === 0 ? (
          <p style={{ color: 'var(--muted)' }}>Nenhum cliente encontrado.</p>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Nome</th>
                <th>Telefone</th>
                <th>Endereço</th>
                <th>Mensalista</th>
                <th>Mensalidade</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {q.data?.itens?.map(c => (
                <tr key={c.id} style={editando?.id === c.id ? { background: 'rgba(99,102,241,0.08)' } : {}}>
                  <td>{c.nome}</td>
                  <td>{c.telefone || '-'}</td>
                  <td>{c.endereco || '-'}</td>
                  <td>{c.mensalista ? 'Sim' : 'Não'}</td>
                  <td>{c.valorMensalidade != null ? `R$ ${Number(c.valorMensalidade).toFixed(2)}` : '-'}</td>
                  <td style={{ display: 'flex', gap: 8 }}>
                    <button className="btn-ghost" onClick={() => iniciarEdicao(c)}>Editar</button>
                    <button className="btn-ghost" onClick={() => {
                      if (window.confirm(`Excluir cliente "${c.nome}"?`)) remover.mutate(c.id)
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
