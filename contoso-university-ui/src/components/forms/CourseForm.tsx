import { useEffect, useState } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import {
  Box,
  TextField,
  Button,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  FormHelperText,
  Alert,
  Typography,
  Card,
  CardMedia,
} from '@mui/material';
import { CloudUpload as UploadIcon } from '@mui/icons-material';
import { useDepartments } from '../../hooks/useDepartments';
import { LoadingSpinner } from '../common/LoadingSpinner';

// File validation constants
const ALLOWED_FILE_TYPES = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/bmp'];
const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB in bytes

// Validation schema
const courseSchema = yup.object({
  courseID: yup
    .number()
    .required('Course ID is required')
    .positive('Course ID must be positive')
    .integer('Course ID must be an integer'),
  title: yup
    .string()
    .required('Title is required')
    .max(50, 'Title must be at most 50 characters'),
  credits: yup
    .number()
    .required('Credits are required')
    .positive('Credits must be positive')
    .integer('Credits must be an integer')
    .min(0, 'Credits must be at least 0')
    .max(5, 'Credits must be at most 5'),
  departmentID: yup
    .number()
    .required('Department is required')
    .positive('Please select a department'),
}).required();

export interface CourseFormData {
  courseID: number;
  title: string;
  credits: number;
  departmentID: number;
  teachingMaterialImage?: File;
}

interface CourseFormProps {
  initialData?: Partial<CourseFormData>;
  existingImagePath?: string;
  onSubmit: (data: CourseFormData) => void;
  onCancel: () => void;
  isSubmitting?: boolean;
  submitLabel?: string;
}

export const CourseForm = ({
  initialData,
  existingImagePath,
  onSubmit,
  onCancel,
  isSubmitting = false,
  submitLabel = 'Save',
}: CourseFormProps) => {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [fileError, setFileError] = useState<string>('');
  const [previewUrl, setPreviewUrl] = useState<string>('');

  // Fetch departments for dropdown
  const { data: departments, isLoading: departmentsLoading } = useDepartments();

  const {
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<CourseFormData>({
    resolver: yupResolver(courseSchema),
    defaultValues: {
      courseID: initialData?.courseID || 0,
      title: initialData?.title || '',
      credits: initialData?.credits || 0,
      departmentID: initialData?.departmentID || 0,
    },
  });

  // Set preview URL for existing image
  useEffect(() => {
    if (existingImagePath) {
      setPreviewUrl(existingImagePath);
    }
  }, [existingImagePath]);

  // Validate file type and size
  const validateFile = (file: File): string | null => {
    if (!ALLOWED_FILE_TYPES.includes(file.type)) {
      return 'Invalid file type. Only JPG, JPEG, PNG, GIF, and BMP files are allowed.';
    }
    if (file.size > MAX_FILE_SIZE) {
      return 'File size exceeds 5MB limit.';
    }
    return null;
  };

  // Handle file selection
  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    setFileError('');
    setSelectedFile(null);
    setPreviewUrl('');

    if (file) {
      const error = validateFile(file);
      if (error) {
        setFileError(error);
        return;
      }

      setSelectedFile(file);
      
      // Create preview URL
      const reader = new FileReader();
      reader.onloadend = () => {
        setPreviewUrl(reader.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  // Handle form submission
  const onFormSubmit = (data: CourseFormData) => {
    // Add the selected file to the form data
    const submitData = {
      ...data,
      teachingMaterialImage: selectedFile || undefined,
    };
    onSubmit(submitData);
  };

  if (departmentsLoading) {
    return <LoadingSpinner message="Loading departments..." />;
  }

  return (
    <Box component="form" onSubmit={handleSubmit(onFormSubmit)} noValidate>
      <Controller
        name="courseID"
        control={control}
        render={({ field }) => (
          <TextField
            {...field}
            label="Course ID"
            type="number"
            fullWidth
            margin="normal"
            error={!!errors.courseID}
            helperText={errors.courseID?.message}
            disabled={!!initialData?.courseID}
            required
          />
        )}
      />

      <Controller
        name="title"
        control={control}
        render={({ field }) => (
          <TextField
            {...field}
            label="Title"
            fullWidth
            margin="normal"
            error={!!errors.title}
            helperText={errors.title?.message}
            required
          />
        )}
      />

      <Controller
        name="credits"
        control={control}
        render={({ field }) => (
          <TextField
            {...field}
            label="Credits"
            type="number"
            fullWidth
            margin="normal"
            error={!!errors.credits}
            helperText={errors.credits?.message}
            required
          />
        )}
      />

      <Controller
        name="departmentID"
        control={control}
        render={({ field }) => (
          <FormControl fullWidth margin="normal" error={!!errors.departmentID} required>
            <InputLabel id="department-label">Department</InputLabel>
            <Select
              {...field}
              labelId="department-label"
              label="Department"
            >
              <MenuItem value={0}>
                <em>Select a department</em>
              </MenuItem>
              {departments?.map((dept) => (
                <MenuItem key={dept.departmentID} value={dept.departmentID}>
                  {dept.name}
                </MenuItem>
              ))}
            </Select>
            {errors.departmentID && (
              <FormHelperText>{errors.departmentID.message}</FormHelperText>
            )}
          </FormControl>
        )}
      />

      <Box sx={{ mt: 3, mb: 2 }}>
        <Typography variant="subtitle1" gutterBottom>
          Teaching Material Image
        </Typography>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          Allowed types: JPG, JPEG, PNG, GIF, BMP (Max size: 5MB)
        </Typography>
        
        <Button
          variant="outlined"
          component="label"
          startIcon={<UploadIcon />}
          sx={{ mt: 1 }}
        >
          Choose File
          <input
            type="file"
            hidden
            accept=".jpg,.jpeg,.png,.gif,.bmp"
            onChange={handleFileChange}
          />
        </Button>

        {selectedFile && (
          <Typography variant="body2" sx={{ mt: 1 }}>
            Selected: {selectedFile.name}
          </Typography>
        )}

        {fileError && (
          <Alert severity="error" sx={{ mt: 2 }}>
            {fileError}
          </Alert>
        )}

        {previewUrl && (
          <Card sx={{ mt: 2, maxWidth: 400 }}>
            <CardMedia
              component="img"
              image={previewUrl}
              alt="Teaching material preview"
              sx={{ maxHeight: 300, objectFit: 'contain' }}
            />
          </Card>
        )}
      </Box>

      <Box sx={{ display: 'flex', gap: 2, mt: 3 }}>
        <Button
          type="submit"
          variant="contained"
          disabled={isSubmitting || !!fileError}
        >
          {isSubmitting ? 'Saving...' : submitLabel}
        </Button>
        <Button
          variant="outlined"
          onClick={onCancel}
          disabled={isSubmitting}
        >
          Cancel
        </Button>
      </Box>
    </Box>
  );
};
