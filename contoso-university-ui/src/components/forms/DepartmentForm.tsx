import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import {
  Box,
  TextField,
  Button,
  Stack,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  FormHelperText,
  Alert,
} from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import dayjs, { Dayjs } from 'dayjs';
import { useInstructors } from '../../hooks/useInstructors';
import { LoadingSpinner } from '../common/LoadingSpinner';
import type { DepartmentCreate, DepartmentUpdate } from '../../types/department';

export interface DepartmentFormData {
  name: string;
  budget: number;
  startDate: Date;
  instructorID?: number | null;
  rowVersion?: string;
}

// Validation schema
const departmentSchema = yup.object({
  name: yup
    .string()
    .required('Department name is required')
    .max(50, 'Department name must be at most 50 characters'),
  budget: yup
    .number()
    .required('Budget is required')
    .typeError('Budget must be a valid number')
    .min(0, 'Budget must be a positive number')
    .max(999999999.99, 'Budget is too large'),
  startDate: yup
    .date()
    .required('Start date is required')
    .typeError('Please enter a valid date'),
  instructorID: yup
    .number()
    .nullable()
    .optional()
    .transform((value, originalValue) => 
      originalValue === '' || originalValue === undefined ? null : value
    ),
  rowVersion: yup.string().optional(),
});

interface DepartmentFormProps {
  initialData?: Partial<DepartmentFormData>;
  onSubmit: (data: DepartmentCreate | DepartmentUpdate) => void;
  onCancel: () => void;
  isSubmitting?: boolean;
  submitLabel?: string;
  departmentId?: number;
}

export const DepartmentForm = ({
  initialData,
  onSubmit,
  onCancel,
  isSubmitting = false,
  submitLabel = 'Save',
  departmentId,
}: DepartmentFormProps) => {
  const {
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<DepartmentFormData>({
    resolver: yupResolver(departmentSchema) as any,
    defaultValues: {
      name: initialData?.name || '',
      budget: initialData?.budget || 0,
      startDate: initialData?.startDate || new Date(),
      instructorID: initialData?.instructorID ?? null,
      rowVersion: initialData?.rowVersion || undefined,
    },
  });

  // Fetch all instructors for the administrator dropdown
  const { data: instructors, isLoading: isLoadingInstructors, error: instructorsError } = useInstructors();

  const handleFormSubmit = (data: DepartmentFormData) => {
    // Convert Date to ISO string for API
    const baseData = {
      name: data.name,
      budget: data.budget,
      startDate: data.startDate.toISOString(),
      instructorID: data.instructorID || undefined,
    };

    // Include departmentID and rowVersion for updates
    const formattedData = departmentId
      ? { 
          ...baseData, 
          departmentID: departmentId,
          rowVersion: data.rowVersion,
        }
      : baseData;

    onSubmit(formattedData as DepartmentCreate | DepartmentUpdate);
  };

  if (isLoadingInstructors) {
    return <LoadingSpinner message="Loading instructors..." />;
  }

  return (
    <LocalizationProvider dateAdapter={AdapterDayjs}>
      <Box component="form" onSubmit={handleSubmit(handleFormSubmit)} noValidate>
        <Stack spacing={3}>
          <Controller
            name="name"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                label="Department Name"
                required
                fullWidth
                error={!!errors.name}
                helperText={errors.name?.message}
                disabled={isSubmitting}
                autoFocus
              />
            )}
          />

          <Controller
            name="budget"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                label="Budget"
                required
                fullWidth
                type="number"
                error={!!errors.budget}
                helperText={errors.budget?.message}
                disabled={isSubmitting}
                inputProps={{
                  step: '0.01',
                  min: '0',
                }}
                InputProps={{
                  startAdornment: '$',
                }}
              />
            )}
          />

          <Controller
            name="startDate"
            control={control}
            render={({ field }) => (
              <DatePicker
                label="Start Date"
                value={field.value ? dayjs(field.value) : null}
                onChange={(newValue: Dayjs | null) => {
                  field.onChange(newValue ? newValue.toDate() : null);
                }}
                disabled={isSubmitting}
                slotProps={{
                  textField: {
                    required: true,
                    fullWidth: true,
                    error: !!errors.startDate,
                    helperText: errors.startDate?.message,
                  },
                }}
              />
            )}
          />

          {instructorsError ? (
            <Alert severity="error">
              Error loading instructors: {instructorsError.message}
            </Alert>
          ) : (
            <Controller
              name="instructorID"
              control={control}
              render={({ field }) => (
                <FormControl fullWidth error={!!errors.instructorID}>
                  <InputLabel id="administrator-label">Administrator</InputLabel>
                  <Select
                    {...field}
                    labelId="administrator-label"
                    label="Administrator"
                    disabled={isSubmitting}
                    value={field.value?.toString() ?? ''}
                    onChange={(e) => {
                      const value = e.target.value;
                      field.onChange(value === '' ? null : Number(value));
                    }}
                  >
                    <MenuItem value="">
                      <em>No Administrator</em>
                    </MenuItem>
                    {instructors && instructors.map((instructor) => (
                      <MenuItem key={instructor.id} value={instructor.id}>
                        {instructor.fullName}
                      </MenuItem>
                    ))}
                  </Select>
                  {errors.instructorID && (
                    <FormHelperText>{errors.instructorID.message}</FormHelperText>
                  )}
                </FormControl>
              )}
            />
          )}

          <Stack direction="row" spacing={2} justifyContent="flex-end">
            <Button
              variant="outlined"
              onClick={onCancel}
              disabled={isSubmitting}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              variant="contained"
              disabled={isSubmitting}
            >
              {submitLabel}
            </Button>
          </Stack>
        </Stack>
      </Box>
    </LocalizationProvider>
  );
};
