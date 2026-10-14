import { Loader2 } from 'lucide-react';

export function LoadingSpinner({ label, fullPage }: { label?: string; fullPage?: boolean }) {
  const content = (
    <div className="flex flex-col items-center justify-center gap-3">
      <Loader2 className="h-8 w-8 animate-spin text-brand-600" />
      {label && <p className="text-sm text-ink-500">{label}</p>}
    </div>
  );

  if (fullPage) {
    return (
      <div className="min-h-[60vh] flex items-center justify-center">{content}</div>
    );
  }
  return content;
}

export function CardSkeleton() {
  return (
    <div className="card p-6">
      <div className="skeleton h-6 w-3/4 rounded-lg mb-4" />
      <div className="skeleton h-4 w-1/2 rounded-lg mb-2" />
      <div className="skeleton h-4 w-2/3 rounded-lg" />
    </div>
  );
}
