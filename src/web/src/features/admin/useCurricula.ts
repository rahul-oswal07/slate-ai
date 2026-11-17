import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/client'

/** A textbook being turned into a draft curriculum (T-146): `GET /api/admin/curriculum-imports`. */
export interface TextbookImport {
  id: string
  title: string
  grade: number
  subject: string
  medium: string
  book: string
  part: number | null
  pageCount: number
  status: 'queued' | 'running' | 'ready' | 'failed'
  stage: 'outline' | 'lessons' | 'prerequisites' | 'assemble' | null
  stepsDone: number
  stepsTotal: number
  /** Short code, e.g. `error:Unavailable reading chapter 3` or `error:stale`; never book text. */
  error: string | null
  notes: string[]
  curriculumId: string | null
  lessons: number | null
  flagged: number | null
}

export type Exercises = {
  compare?: number[][] | null
  extremes?: number[][] | null
  add?: number[][] | null
  sub?: number[][] | null
}

export type Practice = { kind: string; digits: number; carry?: string | null; borrow?: string | null }

/** A lesson as in a curriculum file (`data/curriculum/README.md`). */
export interface CurriculumLesson {
  id: string
  nameKn: string
  nameEn: string
  nameTe?: string | null
  nameTa?: string | null
  nameHi?: string | null
  prerequisites: string[]
  chapter: number
  sequence: number
  examples: string[]
  textbookPage: { book: string; part: number | null; page: number } | null
  keyWordsKn?: string[] | null
  practice?: Practice | null
  exercises?: Exercises | null
}

export type OtherExercise = { page: number; printedPage: number | null; summary: string }
export type PictureNote = { page: number; printedPage: number | null; position: string; description: string; teaches: string }

/** What the importer found besides the lesson itself; pictures are described, never copied (D-125). */
export interface LessonExtras {
  pages: number[]
  printedPages: number[]
  description: string | null
  examples: string[]
  otherExercises: OtherExercise[]
  pictures: PictureNote[]
}

/** Code's answers, in the order of the exercises (guardrail 4). */
export type ExerciseAnswers = { compare: string[]; extremes: number[][]; add: number[]; sub: number[] }

export interface AdminCurriculumLesson {
  lesson: CurriculumLesson
  answers: ExerciseAnswers
  extras: LessonExtras | null
  reviewStatus: 'needs-review' | 'reviewed'
  flags: string[]
}

export interface CurriculumSummary {
  id: string
  title: string
  grade: number
  subject: string
  medium: string
  status: 'draft' | 'published'
  lessons: number
  needsReview: number
  flagged: number
  classes: number
}

export interface CurriculumView {
  summary: CurriculumSummary
  book: string
  part: number | null
  notes: string[]
  chapters: { number: number; nameKn: string; nameEn: string }[]
  lessons: AdminCurriculumLesson[]
  publishProblems: string[]
}

export type ClassChoice = { id: string; label: string; curriculumId: string | null }

/** `GET /api/admin/curricula` (T-147). */
export interface Curricula {
  curricula: CurriculumSummary[]
  classes: ClassChoice[]
  builtInLessons: number
}

/** The body of `PUT …/lessons/{id}`: the lesson's new fields; its ID, chapter and order never change. */
export interface LessonEdit {
  nameKn: string
  nameEn: string
  nameTe: string
  nameTa: string
  nameHi: string
  keyWordsKn: string[]
  page: number | null
  prerequisites: string[]
  practice: Practice | null
  exercises: Exercises
  bookExamples: string[]
  description: string
  examples: string[]
  otherExercises: OtherExercise[]
  pictures: PictureNote[]
  reviewed: boolean
}

const importsKey = ['admin', 'curriculum-imports'] as const
const curriculaKey = ['admin', 'curricula'] as const
const curriculumKey = (id: string) => ['admin', 'curricula', id] as const

/** Every import, checked every 3 s while one is still being read. */
export function useTextbookImports() {
  return useQuery({
    queryKey: importsKey,
    queryFn: () => api<TextbookImport[]>('/api/admin/curriculum-imports'),
    retry: false,
    refetchInterval: (query) =>
      query.state.data?.some((i) => i.status === 'queued' || i.status === 'running') ? 3000 : false,
  })
}

export type UploadFields = { title: string; grade: string; subject: string; medium: string; book: string; part: string; pdf: File }

/** Uploads a textbook (T-146). Refused fields are an `ApiError` 400; too big 413; not a PDF 415. */
export function useUploadTextbook() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (fields: UploadFields) => {
      const form = new FormData()
      form.append('clientRequestId', crypto.randomUUID())
      for (const name of ['title', 'grade', 'subject', 'medium', 'book'] as const) {
        form.append(name, fields[name])
      }
      if (fields.part) {
        form.append('part', fields.part)
      }
      form.append('pdf', fields.pdf)
      return api<TextbookImport>('/api/admin/curriculum-imports', { method: 'POST', form })
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: importsKey }),
  })
}

export function useRetryImport() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) =>
      api<TextbookImport>(`/api/admin/curriculum-imports/${id}/retry`, {
        method: 'POST',
        body: { clientRequestId: crypto.randomUUID() },
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: importsKey }),
  })
}

export function useDiscardImport() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api<undefined>(`/api/admin/curriculum-imports/${id}`, { method: 'DELETE' }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: importsKey })
      await queryClient.invalidateQueries({ queryKey: curriculaKey, exact: true })
    },
  })
}

export function useCurricula() {
  return useQuery({ queryKey: curriculaKey, queryFn: () => api<Curricula>('/api/admin/curricula'), retry: false })
}

export function useCurriculum(id: string) {
  return useQuery({ queryKey: curriculumKey(id), queryFn: () => api<CurriculumView>(`/api/admin/curricula/${id}`), retry: false })
}

/** Saves one draft lesson; a refused edit is an `ApiError` 400 whose body has `errors` by field. */
export function useEditLesson(curriculumId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, edit }: { id: string; edit: LessonEdit }) =>
      api<CurriculumView>(`/api/admin/curricula/${curriculumId}/lessons/${encodeURIComponent(id)}`, {
        method: 'PUT',
        body: { clientRequestId: crypto.randomUUID(), ...edit },
      }),
    onSuccess: (view) => {
      queryClient.setQueryData(curriculumKey(curriculumId), view)
      void queryClient.invalidateQueries({ queryKey: curriculaKey, exact: true })
    },
  })
}

export function useRemoveLesson(curriculumId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) =>
      api<CurriculumView>(
        `/api/admin/curricula/${curriculumId}/lessons/${encodeURIComponent(id)}?clientRequestId=${crypto.randomUUID()}`,
        { method: 'DELETE' },
      ),
    onSuccess: (view) => queryClient.setQueryData(curriculumKey(curriculumId), view),
  })
}

/** Publishes a draft; problems are an `ApiError` 409 whose body has `problems`. */
export function usePublishCurriculum(curriculumId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () =>
      api<CurriculumView>(`/api/admin/curricula/${curriculumId}/publish`, {
        method: 'POST',
        body: { clientRequestId: crypto.randomUUID() },
      }),
    onSuccess: (view) => {
      queryClient.setQueryData(curriculumKey(curriculumId), view)
      void queryClient.invalidateQueries({ queryKey: curriculaKey, exact: true })
    },
  })
}

/** Moves a class to a published curriculum or the built-in one (null); a 409 body has the `missing` lesson IDs. */
export function useSetClassCurriculum() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ classId, curriculumId }: { classId: string; curriculumId: string | null }) =>
      api<ClassChoice>(`/api/admin/classes/${classId}/curriculum`, {
        method: 'PUT',
        body: { clientRequestId: crypto.randomUUID(), curriculumId },
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: curriculaKey }),
  })
}

/** The server's errors by field, as one line for the admin (they are in English, like the IDs). */
export function fieldErrors(body: unknown): string {
  const errors = (body as { errors?: Record<string, string[]> } | null)?.errors ?? {}
  return Object.entries(errors)
    .map(([field, messages]) => `${field}: ${messages.join(' ')}`)
    .join(' · ')
}
