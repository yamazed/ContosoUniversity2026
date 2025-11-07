import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import {
  Box,
  TextField,
  Button,
  Stack,
} from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import dayjs, { Dayjs } from 'dayjs';
import type { StudentCreate, StudentUpdate } from '../../types/student';

// Validation schema
const studentSchema = yup.object({
  lastName: yup
    .string()
    .required('Last name is required')
    .max(50, 'Last name must be at most 50 characters'),
  firstMidName: yup
    .string()
    .required('First name is required')
    .max(50, 'First name must be at most 50 characters'),
  enrollmentDate: yup
    .date()
    .required('Enrollment date is required')
    .typeError('Please enter a valid date')
    .max(new Date(), 'Enrollment date cannot be in the future'),
}).required();

export interface StudentFormData {
  lastName: string;
  firstMidName: string;
  enrollmentDate: Date;
}

interface StudentFormProps {
  initialData?: Partial<StudentFormData>;
  onSubmit: (data: StudentCreate | StudentUpdate) => void;
  onCancel: () => void;
  isSubmitting?: boolean;
  submitLabel?: string;
}

export const StudentForm = ({
  initialData,
  onSubmit,
  onCancel,
  isSubmitting = false,
  submitLabel = 'Save',
}: StudentFormProps) => {
  const {
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<StudentFormData>({
    resolver: yupResolver(studentSchema),
    defaultValues: {
      lastName: initialData?.lastName || '',
      firstMidName: initialData?.firstMidName || '',
      enrollmentDate: initialData?.enrollmentDate || new Date(),
    },
  });

  const handleFormSubmit = (data: StudentFormData) => {
    // Convert Date to ISO string for API
    const formattedData = {
      lastName: data.lastName,
      firstMidName: data.firstMidName,
      enrollmentDate: data.enrollmentDate.toISOString(),
    };
    onSubmit(formattedData);
  };

  return (
    <LocalizationProvider dateAdapter={AdapterDayjs}>
      <Box component="form" onSubmit={handleSubmit(handleFormSubmit)} noValidate>
        <Stack spacing={3}>
          <Controller
            name="lastName"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                label="Last Name"
                required
                fullWidth
                error={!!errors.lastName}
                helperText={errors.lastName?.message}
                disabled={isSubmitting}
                autoFocus
              />
            )}
          />

          <Controller
            name="firstMidName"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                label="First Name"
                required
                fullWidth
                error={!!errors.firstMidName}
                helperText={errors.firstMidName?.message}
                disabled={isSubmitting}
              />
            )}
          />

          <Controller
            name="enrollmentDate"
            control={control}
            render={({ field }) => (
              <DatePicker
                label="Enrollment Date"
                value={field.value ? dayjs(field.value) : null}
                onChange={(newValue: Dayjs | null) => {
                  field.onChange(newValue ? newValue.toDate() : null);
                }}
                disabled={isSubmitting}
                maxDate={dayjs()}
                slotProps={{
                  textField: {
                    required: true,
                    fullWidth: true,
                    error: !!errors.enrollmentDate,
                    helperText: errors.enrollmentDate?.message,
                  },
                }}
              />
            )}
          />

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
