import { useEffect, useRef, useState } from 'react'
import { Upload, Copy, Trash2, Image as ImageIcon, CheckCircle, X } from 'lucide-react'
import { imageApi } from '../../services/api'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { Spinner } from '../../components/ui'
import toast from 'react-hot-toast'
import { format } from 'date-fns'
import { Link } from 'react-router-dom'

export default function AdminImages() {
  const [images, setImages]       = useState([])
  const [loading, setLoading]     = useState(true)
  const [uploading, setUploading] = useState(false)
  const [dragOver, setDragOver]   = useState(false)
  const [copied, setCopied]       = useState(null)
  const fileRef = useRef()

  const load = () => {
    setLoading(true)
    imageApi.getAll()
      .then(r => setImages(r.data))
      .catch(() => toast.error('Failed to load images'))
      .finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [])

  const handleFiles = async (files) => {
    if (!files?.length) return
    const formData = new FormData()
    Array.from(files).forEach(f => formData.append('files', f))
    setUploading(true)
    try {
      const { data } = await imageApi.upload(formData)
      if (data.uploaded?.length)
        toast.success(`${data.uploaded.length} image${data.uploaded.length > 1 ? 's' : ''} uploaded`)
      if (data.errors?.length)
        data.errors.forEach(e => toast.error(e, { duration: 5000 }))
      load()
    } catch (err) {
      toast.error(err.response?.data?.message || 'Upload failed')
    } finally { setUploading(false) }
  }

  const handleDrop = (e) => {
    e.preventDefault()
    setDragOver(false)
    handleFiles(e.dataTransfer.files)
  }

  const handleDelete = async (img) => {
    if (!confirm(`Delete "${img.fileName}"?`)) return
    try {
      await imageApi.delete(img.fileName)
      toast.success('Image deleted')
      setImages(prev => prev.filter(i => i.fileName !== img.fileName))
    } catch {
      toast.error('Failed to delete image')
    }
  }

  const copyUrl = (url, fileName) => {
    navigator.clipboard.writeText(url)
    setCopied(fileName)
    toast.success('URL copied!')
    setTimeout(() => setCopied(null), 2000)
  }

  return (
    <AdminLayout>
      <div className="space-y-6 max-w-[1400px]">

        {/* Header */}
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="font-display text-2xl font-semibold">Image Library</h1>
            <p className="text-surface-400 text-sm mt-0.5">{images.length} images</p>
          </div>
          <div className="flex items-center gap-3">
            <Link to="/admin/products" className="btn-secondary text-sm">← Products</Link>
            <button onClick={() => fileRef.current?.click()} disabled={uploading} className="btn-primary gap-2">
              {uploading ? <Spinner size="sm" /> : <Upload className="w-4 h-4" />}
              <span className="hidden sm:inline">Upload Images</span>
              <span className="sm:hidden">Upload</span>
            </button>
          </div>
          <input ref={fileRef} type="file" multiple accept="image/*" className="hidden"
            onChange={e => handleFiles(e.target.files)} />
        </div>

        {/* Drop zone */}
        <div
          onDragOver={e => { e.preventDefault(); setDragOver(true) }}
          onDragLeave={() => setDragOver(false)}
          onDrop={handleDrop}
          onClick={() => !uploading && fileRef.current?.click()}
          className={`border-2 border-dashed rounded-2xl p-6 sm:p-10 text-center cursor-pointer transition-all duration-200
            ${dragOver ? 'border-brand-400 bg-brand-50' : 'border-surface-200 hover:border-brand-300 hover:bg-surface-50'}`}>
          {uploading ? (
            <div className="flex flex-col items-center gap-3">
              <Spinner size="lg" />
              <p className="text-surface-500 text-sm">Uploading…</p>
            </div>
          ) : (
            <div className="flex flex-col items-center gap-3">
              <div className="w-12 h-12 rounded-2xl bg-brand-50 flex items-center justify-center">
                <Upload className="w-6 h-6 text-brand-500" />
              </div>
              <div>
                <p className="font-medium text-surface-900">
                  {dragOver ? 'Drop to upload' : 'Drag & drop images here'}
                </p>
                <p className="text-surface-400 text-sm mt-0.5">
                  or click to browse · JPG, PNG, WebP, GIF · Max 5 MB each
                </p>
              </div>
            </div>
          )}
        </div>

        {/* Gallery */}
        {loading ? (
          <div className="flex justify-center py-16"><Spinner size="lg" /></div>
        ) : images.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16 gap-3">
            <div className="w-14 h-14 bg-surface-100 rounded-2xl flex items-center justify-center">
              <ImageIcon className="w-7 h-7 text-surface-400" />
            </div>
            <p className="font-medium text-surface-600">No images yet</p>
            <p className="text-surface-400 text-sm">Upload images to use in your product listings</p>
          </div>
        ) : (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4">
            {images.map(img => (
              <div key={img.fileName} className="group card overflow-hidden">
                {/* Thumbnail */}
                <div className="aspect-square bg-surface-50 overflow-hidden relative">
                  <img
                    src={img.url}
                    alt={img.fileName}
                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                  />
                  {/* Hover overlay — desktop only */}
                  <div className="hidden sm:flex absolute inset-0 bg-black/0 group-hover:bg-black/40 transition-all duration-200 items-center justify-center gap-2 opacity-0 group-hover:opacity-100">
                    <button onClick={() => copyUrl(img.url, img.fileName)}
                      className="w-8 h-8 rounded-full bg-white flex items-center justify-center shadow-md hover:bg-brand-50 transition-colors"
                      title="Copy URL">
                      {copied === img.fileName
                        ? <CheckCircle className="w-4 h-4 text-emerald-500" />
                        : <Copy className="w-4 h-4 text-surface-700" />}
                    </button>
                    <button onClick={() => handleDelete(img)}
                      className="w-8 h-8 rounded-full bg-white flex items-center justify-center shadow-md hover:bg-red-50 transition-colors"
                      title="Delete">
                      <Trash2 className="w-4 h-4 text-red-500" />
                    </button>
                  </div>
                  {/* Mobile delete button — always visible */}
                  <button onClick={() => handleDelete(img)}
                    className="sm:hidden absolute top-1.5 right-1.5 w-7 h-7 rounded-full bg-white/90 flex items-center justify-center shadow"
                    title="Delete">
                    <Trash2 className="w-3.5 h-3.5 text-red-500" />
                  </button>
                </div>

                {/* Info */}
                <div className="p-2.5">
                  <p className="text-xs font-medium text-surface-700 truncate" title={img.fileName}>
                    {img.fileName}
                  </p>
                  <div className="flex items-center justify-between mt-0.5">
                    <span className="text-[10px] text-surface-400">{img.sizeFormatted}</span>
                    <span className="text-[10px] text-surface-400">
                      {format(new Date(img.uploadedAt), 'MMM d')}
                    </span>
                  </div>
                  <button
                    onClick={() => copyUrl(img.url, img.fileName)}
                    className={`mt-2 w-full text-xs py-1.5 rounded-lg flex items-center justify-center gap-1.5 transition-colors
                      ${copied === img.fileName
                        ? 'bg-emerald-50 text-emerald-600'
                        : 'bg-surface-100 text-surface-600 hover:bg-brand-50 hover:text-brand-600'}`}>
                    {copied === img.fileName
                      ? <><CheckCircle className="w-3 h-3" /> Copied!</>
                      : <><Copy className="w-3 h-3" /> Copy URL</>}
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </AdminLayout>
  )
}