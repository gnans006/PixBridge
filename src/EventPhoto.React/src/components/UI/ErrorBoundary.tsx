import { Component, type ErrorInfo, type ReactNode } from 'react';
import { AlertTriangle, RefreshCw } from 'lucide-react';

interface Props {
  children: ReactNode;
  /** Optional custom fallback. If omitted, the built-in error card is shown. */
  fallback?: ReactNode;
  /** When true the boundary occupies full page height instead of card height. */
  fullPage?: boolean;
}

interface State {
  hasError: boolean;
  error?: Error;
}

export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false };

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('[ErrorBoundary]', error, info.componentStack);
  }

  handleReset = () => {
    this.setState({ hasError: false, error: undefined });
  };

  render() {
    if (!this.state.hasError) return this.props.children;

    if (this.props.fallback) return this.props.fallback;

    const minHeight = this.props.fullPage ? 'min-h-screen' : 'min-h-[280px]';

    return (
      <div
        className={`flex ${minHeight} flex-col items-center justify-center gap-4 rounded-2xl border border-red-900/30 bg-red-950/20 p-8 text-center`}
      >
        <div className="flex h-14 w-14 items-center justify-center rounded-full bg-red-500/20">
          <AlertTriangle className="h-7 w-7 text-red-400" />
        </div>
        <div>
          <h2 className="text-lg font-semibold text-white">Something went wrong</h2>
          <p className="mt-1 max-w-sm text-sm text-slate-400">
            {this.state.error?.message ?? 'An unexpected error occurred on this page.'}
          </p>
        </div>
        <button
          onClick={this.handleReset}
          className="flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-indigo-500"
        >
          <RefreshCw className="h-4 w-4" />
          Try again
        </button>
      </div>
    );
  }
}
