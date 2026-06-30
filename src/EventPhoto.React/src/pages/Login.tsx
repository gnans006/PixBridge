import { zodResolver } from '@hookform/resolvers/zod';
import { Camera } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { Button } from '../components/UI/Button';
import { Input } from '../components/UI/Input';
import { useAuth } from '../hooks/useAuth';

const schema = z.object({
  username: z.string().min(1, 'Username is required'),
  password: z.string().min(6, 'Password must be at least 6 characters'),
});

type FormData = z.infer<typeof schema>;

export default function Login() {
  const { login, isLoading, error } = useAuth();
  const navigate = useNavigate();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({ resolver: zodResolver(schema) });

  const onSubmit = async (data: FormData) => {
    const isSuccessful = await login(data);
    if (isSuccessful) {
      navigate('/admin');
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-gray-900 to-primary-900 p-4">
      <div className="w-full max-w-md">
        <div className="mb-8 text-center">
          <div className="mb-4 inline-flex h-16 w-16 items-center justify-center rounded-2xl bg-primary-600">
            <Camera className="h-8 w-8 text-white" />
          </div>
          <h1 className="text-3xl font-bold text-white">PixBridge</h1>
          <p className="mt-1 text-gray-400">Event Photo Sharing Platform</p>
        </div>

        <div className="rounded-2xl bg-white p-8 shadow-xl">
          <h2 className="mb-6 text-xl font-semibold text-gray-900">Admin Login</h2>
          {error ? <div className="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-700">{error}</div> : null}
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <Input label="Username" {...register('username')} error={errors.username?.message} autoFocus />
            <Input label="Password" type="password" {...register('password')} error={errors.password?.message} />
            <Button type="submit" className="w-full" isLoading={isLoading}>
              Sign In
            </Button>
          </form>
        </div>
      </div>
    </div>
  );
}
