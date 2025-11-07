import { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';

interface UsePaginationOptions {
  defaultPage?: number;
  syncWithUrl?: boolean;
}

interface UsePaginationReturn {
  page: number;
  setPage: (page: number) => void;
}

/**
 * Hook to manage pagination state with optional URL synchronization
 * @param options - Configuration options
 * @returns Pagination state and setter
 */
export function usePagination(options: UsePaginationOptions = {}): UsePaginationReturn {
  const { defaultPage = 1, syncWithUrl = true } = options;
  const [searchParams, setSearchParams] = useSearchParams();
  const [page, setPageState] = useState<number>(() => {
    if (syncWithUrl) {
      const urlPage = searchParams.get('page');
      return urlPage ? parseInt(urlPage, 10) : defaultPage;
    }
    return defaultPage;
  });

  // Sync with URL on mount and when URL changes
  useEffect(() => {
    if (syncWithUrl) {
      const urlPage = searchParams.get('page');
      const pageNumber = urlPage ? parseInt(urlPage, 10) : defaultPage;
      if (pageNumber !== page && pageNumber >= 1) {
        setPageState(pageNumber);
      }
    }
  }, [searchParams, syncWithUrl, defaultPage]);

  const setPage = (newPage: number) => {
    setPageState(newPage);
    
    if (syncWithUrl) {
      const newParams = new URLSearchParams(searchParams);
      newParams.set('page', newPage.toString());
      setSearchParams(newParams);
    }
  };

  return { page, setPage };
}
