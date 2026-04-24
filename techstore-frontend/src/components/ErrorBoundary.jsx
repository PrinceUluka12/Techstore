import { Component } from 'react'

export default class ErrorBoundary extends Component {
  constructor(props) {
    super(props)
    this.state = { hasError: false, error: null }
  }

  static getDerivedStateFromError(error) {
    return { hasError: true, error }
  }

  componentDidCatch(error, info) {
    console.error('ErrorBoundary caught:', error, info)
  }

  render() {
    if (!this.state.hasError) return this.props.children
    return (
      <div className="min-h-screen flex items-center justify-center flex-col gap-4 p-8 text-center">
        <div className="w-16 h-16 rounded-2xl bg-red-50 flex items-center justify-center mx-auto">
          <span className="text-red-500 text-2xl">!</span>
        </div>
        <h1 className="font-display text-xl font-semibold text-surface-900">Something went wrong</h1>
        <p className="text-surface-500 text-sm max-w-sm">
          {this.state.error?.message || 'An unexpected error occurred.'}
        </p>
        <button onClick={() => window.location.reload()} className="btn-primary">
          Reload Page
        </button>
      </div>
    )
  }
}
