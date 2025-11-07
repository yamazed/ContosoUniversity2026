import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import {
  Box,
  TextField,
  Button,
  Stack,
  FormControl,
  FormLabel,
  FormGroup,
  FormControlLabel,
  Checkbox,
  Typography,
  Divider,
  Alert,
} from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import dayjs, { Dayjs } from 'dayjs';
import { useCourses } from '../../hooks/useCourses';
import { LoadingSpinner } from '../common/LoadingSpinner';
import type { InstructorCreate, InstructorUpdate } from '../../types/instructor';

export interface InstructorFormData {
  lastName: string;
  firstMidName: string;
  hireDate: Date;
  officeLocation: string;
  courseIDs: number[];
}

// Validation schema
const instructorSchema = yup.object({
  lastName: yup
    .string()
    .required('Last name is required')
    .max(50, 'Last name must be at most 50 characters'),
  firstMidName: yup
    .string()
    .required('First name is required')
    .max(50, 'First name must be at most 50 characters'),
  hireDate: yup
    .date()
    .required('Hire date is required')
    .typeError('Please enter a valid date')
    .max(new Date(), 'Hire date cannot be in the future'),
  officeLocation: yup
    .string()
    .max(50, 'Office location must be at most 50 characters')
    .default(''),
  courseIDs: yup
    .array()
    .of(yup.number().required())
    .default([]),
}).required();

interface InstructorFormProps {
  initialData?: Partial<InstructorFormData>;
  onSubmit: (data: InstructorCreate | InstructorUpdate) => void;
  onCancel: () => void;
  isSubmitting?: boolean;
  submitLabel?: string;
  instructorId?: number;
}

export const InstructorForm = ({
  initialData,
  onSubmit,
  onCancel,
  isSubmitting = false,
  submitLabel = 'Save',
  instructorId,
}: InstructorFormProps) => {
  const {
    control,
    handleSubmit,
    formState: { errors },
    watch,
  } = useForm<InstructorFormData>({
    resolver: yupResolver(instructorSchema),
    defaultValues: {
      lastName: initialData?.lastName || '',
      firstMidName: initialData?.firstMidName || '',
      hireDate: initialData?.hireDate || new Date(),
      officeLocation: initialData?.officeLocation || '',
      courseIDs: initialData?.courseIDs || [],
    },
  });

  // Fetch all courses for the multi-select
  const { data: courses, isLoading: isLoadingCourses, error: coursesError } = useCourses();

  const selectedCourseIDs = watch('courseIDs') || [];

  const handleFormSubmit = (data: InstructorFormData) => {
    // Convert Date to ISO string for API
    const baseData = {
      lastName: data.lastName,
      firstMidName: data.firstMidName,
      hireDate: data.hireDate.toISOString(),
      courseIDs: data.courseIDs || [],
    };

    // Add office assignment if location is provided
    const officeAssignment = data.officeLocation && data.officeLocation.trim()
      ? { location: data.officeLocation.trim() }
      : undefined;

    const formattedData = instructorId
      ? { ...baseData, id: instructorId, officeAssignment }
      : { ...baseData, officeAssignment };

    onSubmit(formattedData as InstructorCreate | InstructorUpdate);
  };

  const handleCourseToggle = (courseId: number, onChange: (value: number[]) => void) => {
    const currentIds = selectedCourseIDs;
    const newIds = currentIds.includes(courseId)
      ? currentIds.filter(id => id !== courseId)
      : [...currentIds, courseId];
    onChange(newIds);
  };

  if (isLoadingCourses) {
    return <LoadingSpinner message="Loading courses..." />;
  }

  return (
    <LocalizationProvider dateAdapter={AdapterDayjs}>
      <Box component="form" onSubmit={handleSubmit(handleFormSubmit)} noValidate>
        <Stack spacing={3}>
          {/* Basic Information */}
          <Typography variant="h6">Basic Information</Typography>

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
            name="hireDate"
            control={control}
            render={({ field }) => (
              <DatePicker
                label="Hire Date"
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
                    error: !!errors.hireDate,
                    helperText: errors.hireDate?.message,
                  },
                }}
              />
            )}
          />

          <Divider />

          {/* Office Assignment */}
          <Typography variant="h6">Office Assignment (Optional)</Typography>

          <Controller
            name="officeLocation"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                label="Office Location"
                fullWidth
                error={!!errors.officeLocation}
                helperText={errors.officeLocation?.message || 'Leave blank if no office assigned'}
                disabled={isSubmitting}
                placeholder="e.g., Smith 17"
              />
            )}
          />

          <Divider />

          {/* Course Assignments */}
          <Typography variant="h6">Course Assignments</Typography>

          {coursesError ? (
            <Alert severity="error">
              Error loading courses: {coursesError.message}
            </Alert>
          ) : courses && courses.length > 0 ? (
            <Controller
              name="courseIDs"
              control={control}
              render={({ field }) => (
                <FormControl component="fieldset" variant="standard">
                  <FormLabel component="legend">
                    Select courses to assign to this instructor
                  </FormLabel>
                  <FormGroup>
                    {courses.map((course) => (
                      <FormControlLabel
                        key={course.courseID}
                        control={
                          <Checkbox
                            checked={selectedCourseIDs.includes(course.courseID)}
                            onChange={() => handleCourseToggle(course.courseID, field.onChange)}
                            disabled={isSubmitting}
                          />
                        }
                        label={`${course.courseID} - ${course.title} (${course.departmentName || 'No Department'})`}
                      />
                    ))}
                  </FormGroup>
                </FormControl>
              )}
            />
          ) : (
            <Typography variant="body2" color="text.secondary">
              No courses available for assignment.
            </Typography>
          )}

          <Stack direction="row" spacing={2} justifyContent="flex-end" sx={{ mt: 2 }}>
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
