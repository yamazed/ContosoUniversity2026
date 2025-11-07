import { Box, Pagination as MuiPagination } from '@mui/material';
import { useSearchParams } from 'react-router-dom';
import { useEffect } from 'react';

interface PaginationComponentProps {
  /** Current page number (1-indexed) */
  currentPage: number;
  /** Total number of pages */
  totalPages: number;
  /** Callback when page changes */
  onPageChange: (page: number) => void;
  /** Whether there is a next page (optional, for API compatibility) */
  hasNext?: boolean;
  /** Whether there is a previous page (optional, for API compatibility) */
  hasPrevious?: boolean;
  /** Show first page button */
  showFirstButton?: boolean;
  /** Show last page button */
  showLastButton?: boolean;
  /** Sync page state with URL query parameters */
  syncWithUrl?: boolean;
}

/**
 * Reusable pagination component that integrates with MUI Pagination
 * and optionally syncs page state with URL query parameters.
 * 
 * @example
 * ```tsx
 * <PaginationComponent
 *   currentPage={page}
 *   totalPages={data.totalPages}
 *   onPageChange={setPage}
 *   hasNext={data.hasNextPage}
 *   hasPrevious={data.hasPreviousPage}
 * />
 * ```
 */

export const PaginationComponent = ({
  currentPage,
  totalPages,
  onPageChange,
  hasNext: _hasNext,
  hasPrevious: _hasPrevious,
  showFirstButton = true,
  showLastButton = true,
  syncWithUrl = true,
}: PaginationComponentProps) => {
  // Note: hasNext and hasPrevious are accepted for API compatibility
  // but MUI Pagination automatically handles disabled states based on currentPage and totalPages
  const [searchParams, setSearchParams] = useSearchParams();

  // Sync URL with current page on mount if syncWithUrl is enabled
  useEffect(() => {
    if (syncWithUrl) {
      const urlPage = searchParams.get('page');
      const pageNumber = urlPage ? parseInt(urlPage, 10) : 1;
      
      // If URL page is different from current page and valid, update current page
      if (pageNumber !== currentPage && pageNumber >= 1 && pageNumber <= totalPages) {
        onPageChange(pageNumber);
      } else if (!urlPage || pageNumber < 1 || pageNumber > totalPages) {
        // If no page in URL or invalid page, set it to current page
        const newParams = new URLSearchParams(searchParams);
        newParams.set('page', currentPage.toString());
        setSearchParams(newParams, { replace: true });
      }
    }
  }, []); // Only run on mount

  const handlePageChange = (_event: React.ChangeEvent<unknown>, value: number) => {
    // Update URL query parameter if syncWithUrl is enabled
    if (syncWithUrl) {
      const newParams = new URLSearchParams(searchParams);
      newParams.set('page', value.toString());
      setSearchParams(newParams);
    }
    
    // Call the parent's page change handler
    onPageChange(value);
  };

  // Don't render if there's only one page or no pages
  if (totalPages <= 1) {
    return null;
  }

  return (
    <Box
      sx={{
        display: 'flex',
        justifyContent: 'center',
        mt: 3,
        mb: 2,
      }}
    >
      <MuiPagination
        count={totalPages}
        page={currentPage}
        onChange={handlePageChange}
        color="primary"
        showFirstButton={showFirstButton}
        showLastButton={showLastButton}
        disabled={totalPages === 0}
        siblingCount={1}
        boundaryCount={1}
      />
    </Box>
  );
};
