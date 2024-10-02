namespace ensims.jess_client.Classes {

    public class BeanResponse {
        public bool Ok { get; set; }
        public string Status { get; set; }
    }

    public class BeanAuthResponse :BeanResponse {
        public string Jwt { get; set; }
        public string User { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
    }

    public class BeanVersionInfo {
        public string Title { get; set; }
        public string Description { get; set; }
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Revision { get; set; }
        public string Release { get; set; }
        public int Update { get; set; }
        public string Notice { get; set; }
    }

    public class BeanDataResponse<T>:BeanResponse {
        public T Data { get; set; }
    }

    public class BeanJobStatus {
        public long ID { get; set; }
        public long Job_ID { get; set; }
        public string Status { get; set; }
        public string Status_Info { get; set; }
        public long Completion_Time { get; set; }
        public bool Cancel_Flag { get; set; }
        public long Last_Update { get; set; }
        public string Progress { get; set; }
    }

    public class BeanJobCancelCmd {
        public string cmd { get; set; } = "Cancel";
    }

}
