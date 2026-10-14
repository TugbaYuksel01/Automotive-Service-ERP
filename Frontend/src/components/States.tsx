import { AlertCircle } from 'lucide-react';

export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className="card p-8 text-center">
      <div className="flex items-center justify-center w-12 h-12 rounded-full bg-error-100 mx-auto mb-4">
        <AlertCircle className="h-6 w-6 text-error-600" />
      </div>
      <h3 className="text-lg font-semibold text-ink-900 mb-1">Bir sorun oluştu</h3>
      <p className="text-sm text-ink-500 mb-4">{message}</p>
      {onRetry && (
        <button onClick={onRetry} className="btn-secondary">
          Tekrar Dene
        </button>
      )}
    </div>
  );
}

export function EmptyState({
  icon: Icon,
  title,
  description,
  action,
}: {
  icon: React.ComponentType<{ className?: string }>;
  title: string;
  description: string;
  action?: React.ReactNode;
}) {
  return (
    <div className="card p-12 text-center">
      <div className="flex items-center justify-center w-16 h-16 rounded-2xl bg-ink-100 mx-auto mb-4">
        <Icon className="h-8 w-8 text-ink-400" />
      </div>
      <h3 className="text-lg font-semibold text-ink-900 mb-1">{title}</h3>
      <p className="text-sm text-ink-500 max-w-sm mx-auto mb-4">{description}</p>
      {action}
    </div>
  );
}
