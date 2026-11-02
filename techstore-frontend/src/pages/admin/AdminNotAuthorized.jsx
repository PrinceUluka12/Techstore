import { AdminLayout } from '../../components/layout/AdminLayout'
import { NotAuthorizedMessage } from '../NotAuthorizedPage'

// A back-office page this staff member's role doesn't include. Inside the back office, so the
// menu still shows the pages they can use.
export default function AdminNotAuthorized({ backOffice }) {
  return (
    <AdminLayout>
      <div className="py-6 sm:py-12">
        <NotAuthorizedMessage staff backOffice={backOffice} />
      </div>
    </AdminLayout>
  )
}
