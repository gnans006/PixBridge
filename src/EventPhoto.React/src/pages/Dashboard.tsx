import { useQuery } from '@tanstack/react-query';
import { Calendar, Download, HardDrive, Images } from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import { statisticsApi } from '../api/statistics';
import { Card } from '../components/UI/Card';
import { Spinner } from '../components/UI/Spinner';

function StatCard({ label, value, icon: Icon, color }: { label: string; value: string | number; icon: LucideIcon; color: string }) {
  return (
    <Card className="p-6">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm text-gray-500">{label}</p>
          <p className="mt-1 text-2xl font-bold text-gray-900">{value}</p>
        </div>
        <div className={`flex h-12 w-12 items-center justify-center rounded-xl ${color}`}>
          <Icon className="h-6 w-6 text-white" />
        </div>
      </div>
    </Card>
  );
}

export default function Dashboard() {
  const { data, isLoading } = useQuery({
    queryKey: ['dashboard-stats'],
    queryFn: async () => {
      const response = await statisticsApi.getDashboard();
      return response.data;
    },
    refetchInterval: 30_000,
  });

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Spinner size="lg" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-5">
        <StatCard label="Total Events" value={data?.totalEvents ?? 0} icon={Calendar} color="bg-blue-500" />
        <StatCard label="Active Events" value={data?.activeEvents ?? 0} icon={Calendar} color="bg-green-500" />
        <StatCard label="Total Photos" value={data?.totalPhotos ?? 0} icon={Images} color="bg-purple-500" />
        <StatCard label="Downloads" value={data?.totalDownloads ?? 0} icon={Download} color="bg-pink-500" />
        <StatCard label="Storage Used" value={data?.totalStorageHuman ?? '0 B'} icon={HardDrive} color="bg-orange-500" />
      </div>
    </div>
  );
}
