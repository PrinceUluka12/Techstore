import { useState, useEffect, useCallback } from 'react'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { roleApi } from '../../services/api'
import toast from 'react-hot-toast'
import { Plus, Pencil, Trash2, ShieldCheck, ShieldOff, X } from 'lucide-react'

// ── Permission checkbox group ─────────────────────────────────────────────────
function PermissionGrid({ allPerms, selected, onChange }) {
  const toggle = (key) => {
    const next = selected.includes(key)
      ? selected.filter(p => p !== key)
      : [...selected, key]
    onChange(next)
  }

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
      {allPerms.map(({ key, label }) => (
        <label key={key} className="flex items-center gap-2.5 cursor-pointer group">
          <input
            type="checkbox"
            checked={selected.includes(key)}
            onChange={() => toggle(key)}
            className="w-4 h-4 rounded border-surface-300 text-brand-500 focus:ring-brand-400"
          />
          <span className="text-sm text-surface-700 group-hover:text-surface-900">{label}</span>
        </label>
      ))}
    </div>
  )
}

// ── Create / Edit modal ───────────────────────────────────────────────────────
function RoleModal({ role, allPerms, onClose, onSaved }) {
  const isEdit = Boolean(role)
  const [name, setName] = useState(role?.name ?? '')
  const [description, setDescription] = useState(role?.description ?? '')
  const [permissions, setPermissions] = useState(role?.permissions ?? [])
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
    if (!name.trim()) return toast.error('Name is required.')
    setLoading(true)
    try {
      if (isEdit) {
        const { data } = await roleApi.update(role.id, { description: description.trim() || null, permissions })
        toast.success('Role updated.')
        onSaved(data)
      } else {
        const { data } = await roleApi.create({ name: name.trim(), description: description.trim() || null, permissions })
        toast.success('Role created.')
        onSaved(data)
      }
      onClose()
    } catch (err) {
      toast.error(err.response?.data?.message ?? 'Something went wrong.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[90vh] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-surface-100">
          <h2 className="font-display font-semibold text-surface-900">
            {isEdit ? `Edit "${role.name}"` : 'Create Role'}
          </h2>
          <button onClick={onClose} className="btn-icon"><X className="w-4 h-4" /></button>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="flex flex-col flex-1 overflow-hidden">
          <div className="flex-1 overflow-y-auto px-6 py-4 space-y-4">
            <div>
              <label className="label">Role Name</label>
              <input
                className="input"
                value={name}
                onChange={e => setName(e.target.value)}
                placeholder="e.g. Inventory Manager"
                disabled={isEdit}
                required
              />
              {isEdit && (
                <p className="text-xs text-surface-400 mt-1">Role name cannot be changed after creation.</p>
              )}
            </div>

            <div>
              <label className="label">Description <span className="text-surface-400 font-normal">(optional)</span></label>
              <input
                className="input"
                value={description}
                onChange={e => setDescription(e.target.value)}
                placeholder="What can this role do?"
              />
            </div>

            <div>
              <label className="label mb-2 block">Permissions</label>
              <PermissionGrid allPerms={allPerms} selected={permissions} onChange={setPermissions} />
            </div>
          </div>

          <div className="px-6 py-4 border-t border-surface-100 flex justify-end gap-3">
            <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
            <button type="submit" className="btn-primary" disabled={loading}>
              {loading ? 'Saving…' : isEdit ? 'Save Changes' : 'Create Role'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ── Main page ─────────────────────────────────────────────────────────────────
export default function AdminRoles() {
  const [roles, setRoles] = useState([])
  const [allPerms, setAllPerms] = useState([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState(null) // null | { mode: 'create' } | { mode: 'edit', role }
  const [deleteTarget, setDeleteTarget] = useState(null)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const [rolesRes, permsRes] = await Promise.all([roleApi.getAll(), roleApi.getPermissions()])
      setRoles(rolesRes.data)
      setAllPerms(permsRes.data)
    } catch {
      toast.error('Failed to load roles.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { load() }, [load])

  const handleSaved = (saved) => {
    setRoles(prev => {
      const idx = prev.findIndex(r => r.id === saved.id)
      return idx >= 0 ? prev.map(r => r.id === saved.id ? saved : r) : [saved, ...prev]
    })
  }

  const handleDelete = async () => {
    if (!deleteTarget) return
    try {
      await roleApi.delete(deleteTarget.id)
      setRoles(prev => prev.filter(r => r.id !== deleteTarget.id))
      toast.success(`"${deleteTarget.name}" deleted.`)
    } catch (err) {
      toast.error(err.response?.data?.message ?? 'Cannot delete this role.')
    } finally {
      setDeleteTarget(null)
    }
  }

  return (
    <AdminLayout>
      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="font-display font-bold text-2xl text-surface-900">Roles</h1>
            <p className="text-sm text-surface-500 mt-0.5">Manage custom roles and their permissions.</p>
          </div>
          <button className="btn-primary flex items-center gap-2" onClick={() => setModal({ mode: 'create' })}>
            <Plus className="w-4 h-4" /> New Role
          </button>
        </div>

        {/* Roles list */}
        {loading ? (
          <div className="text-center py-20 text-surface-400">Loading…</div>
        ) : (
          <div className="space-y-3">
            {roles.map(role => (
              <div key={role.id} className="bg-white rounded-xl border border-surface-100 p-5">
                <div className="flex items-start justify-between gap-4">
                  <div className="flex items-center gap-3">
                    {role.isSystem
                      ? <ShieldCheck className="w-5 h-5 text-brand-500 flex-shrink-0" />
                      : <ShieldOff className="w-5 h-5 text-surface-400 flex-shrink-0" />}
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="font-semibold text-surface-900">{role.name}</span>
                        {role.isSystem && (
                          <span className="text-[10px] font-semibold bg-brand-50 text-brand-600 px-2 py-0.5 rounded-full uppercase tracking-wide">
                            System
                          </span>
                        )}
                      </div>
                      {role.description && (
                        <p className="text-sm text-surface-500 mt-0.5">{role.description}</p>
                      )}
                    </div>
                  </div>

                  {!role.isSystem && (
                    <div className="flex items-center gap-2 flex-shrink-0">
                      <button
                        className="btn-icon text-surface-400 hover:text-brand-500"
                        onClick={() => setModal({ mode: 'edit', role })}
                        title="Edit"
                      >
                        <Pencil className="w-4 h-4" />
                      </button>
                      <button
                        className="btn-icon text-surface-400 hover:text-red-500"
                        onClick={() => setDeleteTarget(role)}
                        title="Delete"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  )}
                </div>

                {/* Permissions */}
                {role.permissions.length > 0 ? (
                  <div className="mt-3 flex flex-wrap gap-1.5">
                    {role.permissions.map(p => {
                      const info = allPerms.find(ap => ap.key === p)
                      return (
                        <span key={p} className="text-xs bg-surface-100 text-surface-600 px-2.5 py-1 rounded-full">
                          {info?.label ?? p}
                        </span>
                      )
                    })}
                  </div>
                ) : (
                  <p className="mt-3 text-xs text-surface-400 italic">No permissions assigned.</p>
                )}
              </div>
            ))}

            {roles.length === 0 && (
              <div className="text-center py-20 text-surface-400">No roles yet. Create one above.</div>
            )}
          </div>
        )}
      </div>

      {/* Create / Edit modal */}
      {modal && (
        <RoleModal
          role={modal.mode === 'edit' ? modal.role : null}
          allPerms={allPerms}
          onClose={() => setModal(null)}
          onSaved={handleSaved}
        />
      )}

      {/* Delete confirmation */}
      {deleteTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-sm">
            <h3 className="font-display font-semibold text-surface-900 mb-2">Delete "{deleteTarget.name}"?</h3>
            <p className="text-sm text-surface-500 mb-5">
              This cannot be undone. Users assigned this role will lose their permissions.
            </p>
            <div className="flex justify-end gap-3">
              <button className="btn-secondary" onClick={() => setDeleteTarget(null)}>Cancel</button>
              <button className="btn-danger" onClick={handleDelete}>Delete</button>
            </div>
          </div>
        </div>
      )}
    </AdminLayout>
  )
}
